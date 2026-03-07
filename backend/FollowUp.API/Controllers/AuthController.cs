using AutoMapper;
using FollowUp.Core.Constants;
using FollowUp.Core.DTOs.Auth;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Auth")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;
    private readonly IUserRepository _users;
    private readonly IOtpService _otp;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;
    private readonly IAuditLogService _audit;
    private readonly IMunicipalityRepository _municipalities;

    public AuthController(
        IAuthService auth,
        IUserRepository users,
        IOtpService otp,
        ILogger<AuthController> logger,
        IConfiguration config,
        IMapper mapper,
        IAuditLogService audit,
        IMunicipalityRepository municipalities)
    {
        _auth = auth;
        _users = users;
        _otp = otp;
        _logger = logger;
        _config = config;
        _mapper = mapper;
        _audit = audit;
        _municipalities = municipalities;
    }

    // login with username and password, OTP required on new devices
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "login with username and password")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            // return validation errors
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(
                errors.Any() ? string.Join(", ", errors) : "يرجى التحقق من اسم المستخدم وكلمة المرور"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, token, error) = await _auth.LoginAsync(request.Username, request.Password);
        if (!success)
        {
            // log failed login
            await _audit.LogAsync(null, request.Username, "LoginFailed", error, ipAddress, userAgent);
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تسجيل الدخول"));
        }

        var user = await _users.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            // don't reveal if username exists
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("اسم المستخدم أو كلمة المرور غير صحيحة"));
        }

        // check if active
        if (user.Status != Core.Enums.UserStatus.Active)
        {
            _logger.LogWarning("Login failed - User {UserId} is not active (Status: {Status})", user.UserId, user.Status);
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("حساب المستخدم غير نشط"));
        }

        // device ID from body (mobile) or header (web)
        var deviceId = request.DeviceId ?? Request.Headers["X-Device-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceId) && !Guid.TryParse(deviceId, out _))
        {
            _logger.LogWarning("Invalid device ID format received: {DeviceId}", deviceId);
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("معرّف الجهاز غير صالح"));
        }

        // check if OTP needed
        var requiresOtp = _otp.RequiresOtp(user, deviceId);

        if (requiresOtp)
        {
            // Generate and send OTP
            var (otpSessionToken, demoOtpCode) = await _otp.GenerateAndSendOtpAsync(user, "Login", deviceId);
            if (otpSessionToken == null)
            {
                return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("فشل إرسال رمز التحقق. يرجى المحاولة مرة أخرى"));
            }

            // JWT will be regenerated after OTP verification (not stored in DB)
            _logger.LogInformation("OTP required for user {UserId} ({Role})", user.UserId, user.Role);

            var otpResponse = new LoginResponse
            {
                Success = true,
                RequiresOtp = true,
                SessionToken = otpSessionToken,
                MaskedPhone = _otp.MaskPhoneNumber(user.PhoneNumber),
                User = _mapper.Map<UserDto>(user),
                DemoOtpCode = demoOtpCode // only set when MockSms is enabled
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(otpResponse, "يرجى إدخال رمز التحقق المرسل إلى هاتفك"));
        }

        // No OTP required - return token directly
        // log successful login
        await _audit.LogAsync(user.UserId, user.Username, "Login", "تسجيل دخول ناجح", ipAddress, userAgent);

        // generate refresh token on login
        string? refreshToken = null;
        try
        {
            refreshToken = await _auth.GenerateRefreshTokenAsync(user.UserId, deviceId, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate refresh token for user {UserId} - login will proceed without it", user.UserId);
        }

        var response = new LoginResponse
        {
            Success = true,
            Token = token,
            RefreshToken = refreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            User = _mapper.Map<UserDto>(user)
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "تم تسجيل الدخول بنجاح"));
    }

    // verify OTP code and complete login
    [AllowAnonymous]
    [HttpPost("verify-otp")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "verify otp code and complete login")]
    public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<VerifyOtpResponse>.ErrorResponse(
                errors.Any() ? string.Join(", ", errors) : "يرجى التحقق من رمز التحقق ورمز الجلسة"));
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        // Verify the OTP with IP-based rate limiting
        var (success, userId, error, remaining) = await _otp.VerifyOtpAsync(request.SessionToken, request.OtpCode, ipAddress);

        if (!success)
        {
            return Unauthorized(ApiResponse<VerifyOtpResponse>.ErrorResponse(error ?? "رمز التحقق غير صحيح"));
        }

        var user = await _users.GetByIdAsync(userId!.Value);
        if (user == null)
        {
            _logger.LogError("Data inconsistency: OTP verified for user {UserId} but user not found", userId);
            return StatusCode(500, ApiResponse<VerifyOtpResponse>.ErrorResponse("حدث خطأ في النظام. يرجى المحاولة مرة أخرى أو التواصل مع الدعم الفني"));
        }

        // Check if user was deactivated between login and OTP verification
        if (user.Status != Core.Enums.UserStatus.Active)
        {
            _logger.LogWarning("OTP verification failed - User {UserId} is not active", user.UserId);
            return Unauthorized(ApiResponse<VerifyOtpResponse>.ErrorResponse("حساب المستخدم غير نشط"));
        }

        // If device ID provided, register it for the user (for workers)
        var deviceId = request.DeviceId ?? Request.Headers["X-Device-Id"].FirstOrDefault();

        // Validate device ID format before storing
        if (!string.IsNullOrEmpty(deviceId) && !Guid.TryParse(deviceId, out _))
        {
            _logger.LogWarning("Invalid device ID format in OTP verification: {DeviceId}", deviceId);
            return BadRequest(ApiResponse<VerifyOtpResponse>.ErrorResponse("معرّف الجهاز غير صالح"));
        }

        if (!string.IsNullOrEmpty(deviceId) && string.IsNullOrEmpty(user.RegisteredDeviceId))
        {
            user.RegisteredDeviceId = deviceId;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();
            _logger.LogInformation("Device {DeviceId} registered for user {UserId} after OTP verification", deviceId, user.UserId);
        }

        // Generate a fresh JWT after OTP verification (never store JWT in DB)
        var (tokenSuccess, freshToken, tokenError) = await _auth.GenerateTokenForUserAsync(user);
        if (!tokenSuccess || string.IsNullOrEmpty(freshToken))
        {
            _logger.LogError("Failed to generate JWT after OTP verification for user {UserId}: {Error}", user.UserId, tokenError);
            return StatusCode(500, ApiResponse<VerifyOtpResponse>.ErrorResponse("حدث خطأ أثناء إنشاء رمز الدخول"));
        }

        // Log successful 2FA login
        await _audit.LogAsync(user.UserId, user.Username, "Login2FA", "تسجيل دخول ناجح مع التحقق الثنائي", ipAddress, userAgent);

        // generate refresh token after OTP verification
        string? otpRefreshToken = null;
        try
        {
            otpRefreshToken = await _auth.GenerateRefreshTokenAsync(user.UserId, deviceId, ipAddress);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to generate refresh token after OTP for user {UserId}", user.UserId);
        }

        var response = new VerifyOtpResponse
        {
            Success = true,
            Token = freshToken,
            RefreshToken = otpRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            User = _mapper.Map<UserDto>(user)
        };

        return Ok(ApiResponse<VerifyOtpResponse>.SuccessResponse(response, "تم التحقق بنجاح"));
    }

    // resend OTP code
    [AllowAnonymous]
    [HttpPost("resend-otp")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "resend otp code to user phone")]
    public async Task<IActionResult> ResendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrEmpty(request.SessionToken))
            return BadRequest(ApiResponse<SendOtpResponse>.ErrorResponse("رمز الجلسة مطلوب"));

        // Get user from session (load-balancer safe)
        var userId = await _otp.GetSessionUserIdAsync(request.SessionToken);
        if (userId == null)
        {
            return Unauthorized(ApiResponse<SendOtpResponse>.ErrorResponse("الجلسة غير صالحة. يرجى تسجيل الدخول مرة أخرى"));
        }

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(ApiResponse<SendOtpResponse>.ErrorResponse("المستخدم غير موجود"));
        }

        // Generate and send new OTP (JWT is regenerated fresh after verification, not stored)
        var (newSessionToken, resendDemoOtp) = await _otp.GenerateAndSendOtpAsync(user, "Login", request.DeviceId);
        if (newSessionToken == null)
        {
            return StatusCode(500, ApiResponse<SendOtpResponse>.ErrorResponse("فشل إرسال رمز التحقق"));
        }

        var response = new SendOtpResponse
        {
            Success = true,
            SessionToken = newSessionToken, // client must use this new token for verify-otp
            MaskedPhone = _otp.MaskPhoneNumber(user.PhoneNumber),
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.OtpExpirationMinutes),
            Message = "تم إرسال رمز التحقق",
            ResendCooldownSeconds = AppConstants.OtpResendCooldownSeconds,
            DemoOtpCode = resendDemoOtp
        };

        return Ok(ApiResponse<SendOtpResponse>.SuccessResponse(response, "تم إرسال رمز التحقق"));
    }

    // GPS-based login for mobile workers
    // authenticates credentials, validates device binding, returns JWT token
    // attendance is handled separately via geofencing in the mobile background service
    [AllowAnonymous]
    [HttpPost("login-gps")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "gps-based login for mobile workers")]
    public async Task<IActionResult> LoginWithGPS([FromBody] LoginWithGPSRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<LoginWithGPSResponse>.ErrorResponse(errors));
            }

            // authenticate user with username and password
            var (authSuccess, token, authError) = await _auth.LoginAsync(request.Username, request.Password);

            if (!authSuccess)
            {
                _logger.LogWarning("GPS login failed - Invalid credentials for user {Username}", request.Username);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse(authError ?? "اسم المستخدم أو كلمة المرور غير صحيحة"));
            }

            // get user details
            var user = await _users.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                // don't reveal if username exists
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("اسم المستخدم أو كلمة المرور غير صحيحة"));
            }

            // check if user is active or not
            if (user.Status != Core.Enums.UserStatus.Active)
            {
                _logger.LogWarning("GPS login failed - User {UserId} is not active", user.UserId);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("حساب المستخدم غير نشط"));
            }

            // Validate device ID format
            if (!string.IsNullOrEmpty(request.DeviceId) && !Guid.TryParse(request.DeviceId, out _))
            {
                _logger.LogWarning("Invalid device ID format in GPS login: {DeviceId}", request.DeviceId);
                return BadRequest(ApiResponse<LoginWithGPSResponse>.ErrorResponse("معرّف الجهاز غير صالح"));
            }

            // device binding: lock account to one phone, reject if different device
            var disableDeviceBinding = _config.GetValue<bool>("DeveloperMode:DisableDeviceBinding");
            if (disableDeviceBinding)
            {
                _logger.LogWarning("SECURITY: Device binding is DISABLED. Set DeveloperMode:DisableDeviceBinding=false for production!");
            }
            else
            {
                if (string.IsNullOrEmpty(user.RegisteredDeviceId))
                {
                    // First login - register this device
                    user.RegisteredDeviceId = request.DeviceId;
                    _logger.LogInformation("Device registered for user {UserId}: {DeviceId}", user.UserId, request.DeviceId);
                }
                else if (user.RegisteredDeviceId != request.DeviceId)
                {
                    // Different device trying to login - reject for security (2FA failed)
                    _logger.LogWarning("Device binding failed - User {UserId} attempted login from unregistered device. Expected: {Expected}, Got: {Got}",
                        user.UserId, user.RegisteredDeviceId, request.DeviceId);
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("هذا الجهاز غير مسجل لهذا الحساب. يرجى التواصل مع المشرف."));
                }
            }

            // login is authentication only - no attendance check-in here
            // attendance is handled automatically via geofencing in the mobile background service

            // update user last login and save device registration if new
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();

            // generate refresh token for GPS login (consistency with other login flows)
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
            string? gpsRefreshToken = null;
            try
            {
                gpsRefreshToken = await _auth.GenerateRefreshTokenAsync(user.UserId, request.DeviceId, ipAddress);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate refresh token for GPS login user {UserId}", user.UserId);
            }

            var response = new LoginWithGPSResponse
            {
                Success = true,
                Token = token,
                RefreshToken = gpsRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = _mapper.Map<UserDto>(user),
                Message = "تم تسجيل الدخول بنجاح"
            };

            return Ok(ApiResponse<LoginWithGPSResponse>.SuccessResponse(response, "تم تسجيل الدخول بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GPS login");
            return StatusCode(500, ApiResponse<LoginWithGPSResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

    // register new user only admin can do this
    [HttpPost("register")]
    //[AllowAnonymous] // SECURITY FIX: Removed - Anyone could become admin!
    [Authorize(Roles = "Admin")] // REQUIRED: Only admins can create users
    [SwaggerOperation(Summary = "register a new user (admin only)")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        // SECURITY NOTE: If you need to create the first admin:
        // 1. Temporarily enable [AllowAnonymous] above
        // 2. Create ONE admin account
        // 3. IMMEDIATELY re-enable [Authorize(Roles = "Admin")]
        // 4. Rebuild and redeploy

        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(errors));
        }

        // check password strength
        if (!Utils.InputSanitizer.IsStrongPassword(request.Password))
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص"));

        // Validate phone number format (must start with + and have 10-15 digits)
        if (!string.IsNullOrEmpty(request.PhoneNumber))
        {
            var cleanPhone = request.PhoneNumber.Replace("-", "").Replace(" ", "");
            if (!System.Text.RegularExpressions.Regex.IsMatch(cleanPhone, @"^\+\d{10,15}$"))
                return BadRequest(ApiResponse<UserDto>.ErrorResponse("رقم الهاتف غير صالح. يجب أن يبدأ بـ + ويحتوي 10-15 رقم"));
        }

        // Validate: Workers must have a supervisor assigned
        if (request.Role == Core.Enums.UserRole.Worker && !request.SupervisorId.HasValue)
            return BadRequest(ApiResponse<UserDto>.ErrorResponse("يجب تحديد المشرف عند إنشاء حساب عامل"));

        // Bootstrap: Auto-create default municipality if none exists (first-time setup only)
        var allMunicipalities = await _municipalities.GetAllAsync();
        var municipality = allMunicipalities.FirstOrDefault();
        if (municipality == null)
        {
            var municipalitySettings = _config.GetSection("MunicipalitySettings");
            municipality = new Municipality
            {
                Code = municipalitySettings["Code"] ?? "DEFAULT",
                Name = municipalitySettings["Name"] ?? "FollowUp",
                NameEnglish = municipalitySettings["NameEnglish"] ?? "FollowUp System",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _municipalities.AddAsync(municipality);
            await _municipalities.SaveChangesAsync();
            _logger.LogWarning("Auto-created default municipality '{Name}' during first user registration", municipality.Name);
        }

        // create user object
        var user = new User
        {
            Username = request.Username,
            FullName = Utils.InputSanitizer.SanitizeString(request.FullName, 100),
            Email = request.Role != Core.Enums.UserRole.Worker ? request.Email : null,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role,
            WorkerType = request.WorkerType,
            DepartmentId = request.DepartmentId,
            TeamId = request.TeamId,
            MunicipalityId = municipality.MunicipalityId,
            SupervisorId = request.Role == Core.Enums.UserRole.Worker ? request.SupervisorId : null
        };

        var (success, createdUser, error) = await _auth.RegisterAsync(user, request.Password);
        if (!success)
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(error ?? "فشل التسجيل"));

        // audit log for user creation
        var currentUserId = GetCurrentUserId();
        var currentUser = currentUserId.HasValue ? await _users.GetByIdAsync(currentUserId.Value) : null;
        await _audit.LogAsync(currentUserId, currentUser?.Username, "UserCreated",
            $"إنشاء مستخدم جديد: {createdUser!.Username} ({createdUser.Role})",
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            Request.Headers.UserAgent.ToString());

        var userDto = _mapper.Map<UserDto>(createdUser!);
        return CreatedAtAction(nameof(GetProfile), null, ApiResponse<UserDto>.SuccessResponse(userDto, "تم تسجيل المستخدم بنجاح"));
    }

    // get current user profile (alias: /me for frontend compatibility)
    [HttpGet("me")]
    [HttpGet("profile")]
    [SwaggerOperation(Summary = "get current user profile")]
    public async Task<IActionResult> GetProfile()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("رمز دخول غير صالح"));

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.ErrorResponse("المستخدم غير موجود"));

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
    }

    // refresh access token using refresh token
    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "refresh access token using refresh token")]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
        if (string.IsNullOrEmpty(request?.RefreshToken))
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("رمز التحديث مطلوب"));

        var (success, accessToken, newRefreshToken, error) = await _auth.RefreshAccessTokenAsync(request.RefreshToken);

        if (!success)
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تحديث الرمز"));

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(new LoginResponse
        {
            Success = true,
            Token = accessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes())
        }, "تم تحديث الرمز بنجاح"));
    }

    // logout the user
    [HttpPost("logout")]
    [Authorize]
    [SwaggerOperation(Summary = "logout the current user")]
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            await _auth.LogoutAsync(userId.Value);

            // audit log for logout
            var user = await _users.GetByIdAsync(userId.Value);
            await _audit.LogAsync(userId, user?.Username, "Logout",
                "تسجيل خروج",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }

        return Ok(ApiResponse<string>.SuccessResponse("تم تسجيل الخروج بنجاح"));
    }

    // forgot password - send OTP to user's phone
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "send password reset otp to phone")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<SendOtpResponse>.ErrorResponse("اسم المستخدم مطلوب"));

        var user = await _users.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            // don't reveal if username exists
            return Ok(ApiResponse<SendOtpResponse>.SuccessResponse(new SendOtpResponse
            {
                Success = true,
                MaskedPhone = "****",
                Message = "إذا كان الحساب موجوداً، سيتم إرسال رمز التحقق"
            }));
        }

        if (string.IsNullOrEmpty(user.PhoneNumber))
        {
            return Ok(ApiResponse<SendOtpResponse>.SuccessResponse(new SendOtpResponse
            {
                Success = true,
                MaskedPhone = "****",
                Message = "إذا كان الحساب موجوداً، سيتم إرسال رمز التحقق"
            }));
        }

        var (resetSessionToken, resetDemoOtp) = await _otp.GenerateAndSendOtpAsync(user, "PasswordReset");
        if (resetSessionToken == null)
        {
            return StatusCode(500, ApiResponse<SendOtpResponse>.ErrorResponse("فشل إرسال رمز التحقق"));
        }

        _logger.LogInformation("Password reset OTP sent to user {UserId}", user.UserId);

        return Ok(ApiResponse<SendOtpResponse>.SuccessResponse(new SendOtpResponse
        {
            Success = true,
            SessionToken = resetSessionToken,
            MaskedPhone = _otp.MaskPhoneNumber(user.PhoneNumber),
            Message = "تم إرسال رمز التحقق",
            ExpiresAt = DateTime.UtcNow.AddMinutes(Core.Constants.AppConstants.OtpExpirationMinutes),
            ResendCooldownSeconds = Core.Constants.AppConstants.OtpResendCooldownSeconds,
            DemoOtpCode = resetDemoOtp // only set when MockSms is enabled
        }));
    }

    // reset password using OTP verification
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
    [SwaggerOperation(Summary = "reset password using otp verification")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse(string.Join(", ", errors)));
        }

        // check password strength
        if (!Utils.InputSanitizer.IsStrongPassword(request.NewPassword))
            return BadRequest(ApiResponse<object>.ErrorResponse("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم ورمز خاص"));

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        // verify OTP
        var (success, userId, error, _) = await _otp.VerifyOtpAsync(request.SessionToken, request.OtpCode, ipAddress);
        if (!success || !userId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse(error ?? "رمز التحقق غير صحيح"));

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // update password
        user.PasswordHash = _auth.HashPassword(request.NewPassword);
        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        // audit log
        await _audit.LogAsync(userId, user.Username, "PasswordReset",
            "إعادة تعيين كلمة المرور عبر رمز التحقق",
            ipAddress,
            Request.Headers.UserAgent.ToString());

        _logger.LogInformation("Password reset completed for user {UserId}", userId.Value);

        return Ok(ApiResponse<object>.SuccessResponse(null, "تم إعادة تعيين كلمة المرور بنجاح"));
    }

    // save fcm token for push notifications
    [HttpPost("register-fcm-token")]
    [Authorize]
    [SwaggerOperation(Summary = "save fcm token for push notifications")]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .ToList();
            return BadRequest(ApiResponse<object>.ErrorResponse(
                errors.Any() ? string.Join(", ", errors) : "يرجى التحقق من رمز FCM"));
        }

        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز مستخدم غير صالح"));

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // save the token to user
        user.FcmToken = request.FcmToken;
        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تسجيل رمز الإشعارات بنجاح"));
    }

    // get token expiry from config
    private int GetTokenExpirationMinutes()
    {
        return int.TryParse(_config["JwtSettings:ExpirationMinutes"], out var expMin) ? expMin : 1440;
    }
}
