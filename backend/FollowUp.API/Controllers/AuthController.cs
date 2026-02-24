using AutoMapper;
using FollowUp.Core.Constants;
using FollowUp.Core.DTOs.Auth;
using FollowUp.Core.DTOs.Attendance;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using FollowUp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly IAttendanceRepository _attendance;
    private readonly IOtpService _otp;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;
    private readonly IMapper _mapper;
    private readonly AuditLogService _audit;

    public AuthController(
        IAuthService auth,
        IUserRepository users,
        IZoneRepository zones,
        IAttendanceRepository attendance,
        IOtpService otp,
        ILogger<AuthController> logger,
        IConfiguration config,
        IMapper mapper,
        AuditLogService audit)
    {
        _auth = auth;
        _users = users;
        _zones = zones;
        _attendance = attendance;
        _otp = otp;
        _logger = logger;
        _config = config;
        _mapper = mapper;
        _audit = audit;
    }

    // this method handle normal login with username and password
    // For Admin/Supervisor: Always requires OTP
    // For Worker: OTP only on new device
    [AllowAnonymous]
    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            // Provide specific validation error details
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
            // UR23: Log failed login
            await _audit.LogAsync(null, request.Username, "LoginFailed", error, ipAddress, userAgent);
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تسجيل الدخول"));
        }

        var user = await _users.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            // SECURITY: Don't reveal if username exists - use generic message
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("اسم المستخدم أو كلمة المرور غير صحيحة"));
        }

        // Validate device ID format (must be valid GUID)
        // Accept from request body (mobile sends it there) or X-Device-Id header (fallback)
        var deviceId = request.DeviceId ?? Request.Headers["X-Device-Id"].FirstOrDefault();
        if (!string.IsNullOrEmpty(deviceId) && !Guid.TryParse(deviceId, out _))
        {
            _logger.LogWarning("Invalid device ID format received: {DeviceId}", deviceId);
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("معرّف الجهاز غير صالح"));
        }

        // Check if OTP is required (Admin/Supervisor always, Worker on new device)
        var requiresOtp = _otp.RequiresOtp(user, deviceId);

        if (requiresOtp)
        {
            // Generate and send OTP
            var sessionToken = await _otp.GenerateAndSendOtpAsync(user, "Login", deviceId);
            if (sessionToken == null)
            {
                return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("فشل إرسال رمز التحقق. يرجى المحاولة مرة أخرى"));
            }

            // Store pending JWT token in database (load-balancer safe)
            try
            {
                await _otp.StorePendingTokenAsync(sessionToken, token!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to store pending JWT token for user {UserId}", user.UserId);
                return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول. يرجى المحاولة مرة أخرى"));
            }

            _logger.LogInformation("OTP required for user {UserId} ({Role})", user.UserId, user.Role);

            var otpResponse = new LoginResponse
            {
                Success = true,
                RequiresOtp = true,
                SessionToken = sessionToken,
                MaskedPhone = _otp.MaskPhoneNumber(user.PhoneNumber),
                User = _mapper.Map<UserDto>(user) // Include basic user info
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(otpResponse, "يرجى إدخال رمز التحقق المرسل إلى هاتفك"));
        }

        // No OTP required - return token directly
        // UR23: Log successful login
        await _audit.LogAsync(user.UserId, user.Username, "Login", "تسجيل دخول ناجح", ipAddress, userAgent);

        // ERD Chapter 3: Generate refresh token on login
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

    /// <summary>
    /// Verify OTP code and complete login
    /// </summary>
    [AllowAnonymous]
    [HttpPost("verify-otp")]
    [EnableRateLimiting("auth")]
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
            var failResponse = new VerifyOtpResponse
            {
                Success = false,
                Error = error,
                RemainingAttempts = remaining
            };
            return Unauthorized(ApiResponse<VerifyOtpResponse>.ErrorResponse(error ?? "رمز التحقق غير صحيح"));
        }

        // Get the stored JWT token from database (load-balancer safe)
        var storedToken = await _otp.GetAndClearPendingTokenAsync(request.SessionToken);
        if (string.IsNullOrEmpty(storedToken))
        {
            return Unauthorized(ApiResponse<VerifyOtpResponse>.ErrorResponse("انتهت صلاحية الجلسة. يرجى تسجيل الدخول مرة أخرى"));
        }

        var user = await _users.GetByIdAsync(userId!.Value);
        if (user == null)
        {
            // This should never happen - OTP was valid but user doesn't exist
            // Indicates data inconsistency (user was deleted after OTP was generated)
            _logger.LogError("Data inconsistency: OTP verified for user {UserId} but user not found", userId);
            return StatusCode(500, ApiResponse<VerifyOtpResponse>.ErrorResponse("حدث خطأ في النظام. يرجى المحاولة مرة أخرى أو التواصل مع الدعم الفني"));
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

        // Log successful 2FA login
        await _audit.LogAsync(user.UserId, user.Username, "Login2FA", "تسجيل دخول ناجح مع التحقق الثنائي", ipAddress, userAgent);

        // ERD Chapter 3: Generate refresh token after OTP verification
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
            Token = storedToken,
            RefreshToken = otpRefreshToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
            User = _mapper.Map<UserDto>(user)
        };

        return Ok(ApiResponse<VerifyOtpResponse>.SuccessResponse(response, "تم التحقق بنجاح"));
    }

    /// <summary>
    /// Resend OTP code
    /// </summary>
    [AllowAnonymous]
    [HttpPost("resend-otp")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ResendOtp([FromBody] SendOtpRequest request)
    {
        if (string.IsNullOrEmpty(request.SessionToken))
            return BadRequest(ApiResponse<SendOtpResponse>.ErrorResponse("رمز الجلسة مطلوب"));

        // Get session info from database (load-balancer safe)
        var (userId, jwtToken) = await _otp.GetSessionInfoAsync(request.SessionToken);
        if (userId == null || string.IsNullOrEmpty(jwtToken))
        {
            return Unauthorized(ApiResponse<SendOtpResponse>.ErrorResponse("الجلسة غير صالحة. يرجى تسجيل الدخول مرة أخرى"));
        }

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
        {
            return Unauthorized(ApiResponse<SendOtpResponse>.ErrorResponse("المستخدم غير موجود"));
        }

        // Generate and send new OTP
        var newSessionToken = await _otp.GenerateAndSendOtpAsync(user, "Login", request.DeviceId);
        if (newSessionToken == null)
        {
            return StatusCode(500, ApiResponse<SendOtpResponse>.ErrorResponse("فشل إرسال رمز التحقق"));
        }

        // Store the JWT token in the new session
        await _otp.StorePendingTokenAsync(newSessionToken, jwtToken);

        var response = new SendOtpResponse
        {
            Success = true,
            MaskedPhone = _otp.MaskPhoneNumber(user.PhoneNumber),
            ExpiresAt = DateTime.UtcNow.AddMinutes(AppConstants.OtpExpirationMinutes),
            Message = "تم إرسال رمز التحقق",
            ResendCooldownSeconds = AppConstants.OtpResendCooldownSeconds
        };

        return Ok(ApiResponse<SendOtpResponse>.SuccessResponse(response, "تم إرسال رمز التحقق"));
    }

    /// <summary>
    /// GPS-Based Login and Attendance - Mobile Worker Authentication Flow
    ///
    /// This endpoint handles worker login with automatic attendance check-in.
    /// Per UC2: Login is pure authentication. Attendance is handled separately via geofencing (UC4).
    ///
    /// Security Features:
    /// - Device binding: Workers can only login from their registered device
    ///
    /// Flow:
    /// 1. Authenticate credentials (username + password)
    /// 2. Validate device ID matches registered device
    /// 3. Return JWT token
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login-gps")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> LoginWithGPS([FromBody] LoginWithGPSRequest request)
    {
        try
        {
            // check if request data is valid
            if (!ModelState.IsValid)
            {
                var errors = new List<string>();
                foreach (var modelState in ModelState.Values)
                {
                    foreach (var error in modelState.Errors)
                    {
                        errors.Add(error.ErrorMessage);
                    }
                }
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
                // SECURITY: Don't reveal if username exists - use generic message
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

            /*
             * DEVICE BINDING SECURITY (2FA)
             *
             * Purpose: Prevent account theft by binding user account to their physical mobile device
             *
             * How it works:
             * 1. First Login: Device ID is automatically registered for the user
             * 2. Subsequent Logins: Device ID must match the registered device
             * 3. If different device: Login is rejected (requires admin to reset device binding)
             *
             * This adds a layer of security - even if someone steals the password,
             * they cannot login from a different device without admin intervention.
             *
             * Note: Can be disabled in development mode for testing purposes
             */
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

            // UC2: Login is authentication only. No attendance check-in here.
            // Attendance is handled automatically via geofencing (UC4) in the mobile background service.

            // update user last login and save device registration if new
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();

            // ERD Chapter 3: Generate refresh token for GPS login (consistency with other login flows)
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

            // build response object - authentication only, no attendance
            var resObj = new LoginWithGPSResponse
            {
                Success = true,
                Token = token,
                RefreshToken = gpsRefreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = _mapper.Map<UserDto>(user),
                Message = "تم تسجيل الدخول بنجاح"
            };

            return Ok(ApiResponse<LoginWithGPSResponse>.SuccessResponse(resObj, "تم تسجيل الدخول بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GPS login");
            return StatusCode(500, ApiResponse<LoginWithGPSResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

    #region GPS Validation Helper Methods

    /// <summary>
    /// Validates GPS accuracy is within acceptable range
    /// </summary>
    /// <param name="accuracy">GPS accuracy in meters</param>
    /// <returns>Error message if invalid, null if valid</returns>
    private string? ValidateGpsAccuracy(double? accuracy)
    {
        if (accuracy.HasValue && accuracy.Value > Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters)
        {
            return $"دقة GPS منخفضة جداً ({accuracy.Value:F1}م). يرجى المحاولة في مكان أفضل.";
        }
        return null;
    }

    /// <summary>
    /// Validates GPS coordinates are within municipality bounds
    /// Supports both production and testing mode
    /// </summary>
    private string? ValidateGpsBounds(double latitude, double longitude)
    {
        var isTestingMode = _config.GetValue<bool>("DeveloperMode:DisableGeofencing");

        double minLat, maxLat, minLon, maxLon;
        if (isTestingMode)
        {
            // Extended bounds for testing (includes Birzeit University)
            minLat = Core.Constants.GeofencingConstants.TestingMinLatitude;
            maxLat = Core.Constants.GeofencingConstants.TestingMaxLatitude;
            minLon = Core.Constants.GeofencingConstants.TestingMinLongitude;
            maxLon = Core.Constants.GeofencingConstants.TestingMaxLongitude;
        }
        else
        {
            // Production bounds - municipality area only
            minLat = Core.Constants.GeofencingConstants.MinLatitude;
            maxLat = Core.Constants.GeofencingConstants.MaxLatitude;
            minLon = Core.Constants.GeofencingConstants.MinLongitude;
            maxLon = Core.Constants.GeofencingConstants.MaxLongitude;
        }

        if (latitude < minLat || latitude > maxLat || longitude < minLon || longitude > maxLon)
        {
            return "الموقع خارج منطقة الخدمة. يرجى التحقق من GPS.";
        }

        return null;
    }

    /// <summary>
    /// Finds the zone that contains the given GPS coordinates for the specified user
    /// Checks user's assigned zones only
    /// </summary>
    /// <returns>Matched zone if found, null otherwise</returns>
    private async Task<(Zone? MatchedZone, string? Error)> FindUserZoneByGps(int userId, double latitude, double longitude)
    {
        var disableGeofencing = _config.GetValue<bool>("DeveloperMode:DisableGeofencing");

        // Get user's assigned zones
        var userWithZones = await _users.GetUserWithZonesAsync(userId);
        var assignedZoneIds = userWithZones?.AssignedZones?
            .Where(uz => uz.IsActive)
            .Select(uz => uz.ZoneId)
            .ToList() ?? new List<int>();

        if (!assignedZoneIds.Any())
        {
            return (null, "لا يوجد مناطق عمل مخصصة لهذا المستخدم.");
        }

        var assignedZones = (await _zones.GetZonesByIdsAsync(assignedZoneIds))
            .Where(z => z.IsActive)
            .ToList();

        if (disableGeofencing)
        {
            // In testing mode, return first assigned zone without location validation
            _logger.LogWarning("DEVELOPMENT MODE: Geofencing disabled - accepting any location for user {UserId}", userId);
            return (assignedZones.FirstOrDefault(), null);
        }

        /*
         * ZONE MATCHING ALGORITHM
         *
         * Uses geospatial point-in-polygon algorithm to determine if worker
         * is physically present inside their assigned work zone.
         *
         * Algorithm:
         * 1. Convert GPS coordinates (lat/lon) to a geometric Point
         * 2. For each assigned zone:
         *    a. Check if point is inside zone boundary (Contains)
         *    b. OR if point is near zone boundary (within buffer tolerance)
         * 3. Return first matching zone
         *
         * Buffer Tolerance:
         * - Allows small GPS inaccuracies near zone edges
         * - Prevents false rejections due to GPS drift
         * - Configured in GeofencingConstants.BufferToleranceDegrees
         *
         * Library: Uses NetTopologySuite for geospatial operations
         */
        var point = NetTopologySuite.Geometries.GeometryFactory.Default.CreatePoint(
            new NetTopologySuite.Geometries.Coordinate(longitude, latitude));

        foreach (var zone in assignedZones)
        {
            if (zone.Boundary == null) continue;

            // Check if point is inside zone or within buffer tolerance
            if (zone.Boundary.Contains(point) ||
                zone.Boundary.Distance(point) <= Core.Constants.GeofencingConstants.BufferToleranceDegrees)
            {
                return (zone, null);
            }
        }

        return (null, "أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور.");
    }

    #endregion

    // register new user only admin can do this
    [HttpPost("register")]
    //[AllowAnonymous] // SECURITY FIX: Removed - Anyone could become admin!
    [Authorize(Roles = "Admin")] // REQUIRED: Only admins can create users
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

        // TEMP: Auto-create default municipality if none exists
        var dbContext = HttpContext.RequestServices.GetRequiredService<FollowUp.Infrastructure.Data.FollowUpDbContext>();
        var municipality = await dbContext.Municipalities.FirstOrDefaultAsync();
        if (municipality == null)
        {
            // Get default municipality settings from configuration
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
            dbContext.Municipalities.Add(municipality);
            await dbContext.SaveChangesAsync();
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

        // UC17: Audit log for user creation
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
    [Authorize]
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

    // ERD Chapter 3: Refresh access token using refresh token
    [AllowAnonymous]
    [HttpPost("refresh")]
    [EnableRateLimiting("auth")]
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
    public async Task<IActionResult> Logout()
    {
        var userId = GetCurrentUserId();
        if (userId.HasValue)
        {
            await _auth.LogoutAsync(userId.Value);

            // UC17: Audit log for logout
            var user = await _users.GetByIdAsync(userId.Value);
            await _audit.LogAsync(userId, user?.Username, "Logout",
                "تسجيل خروج",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }

        return Ok(ApiResponse<string>.SuccessResponse("تم تسجيل الخروج بنجاح"));
    }

    // SR1.6: Forgot password - send OTP to user's phone
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<SendOtpResponse>.ErrorResponse("اسم المستخدم مطلوب"));

        var user = await _users.GetByUsernameAsync(request.Username);
        if (user == null)
        {
            // SECURITY: Don't reveal if username exists - always return success
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

        var sessionToken = await _otp.GenerateAndSendOtpAsync(user, "PasswordReset");
        if (sessionToken == null)
        {
            return StatusCode(500, ApiResponse<SendOtpResponse>.ErrorResponse("فشل إرسال رمز التحقق"));
        }

        _logger.LogInformation("Password reset OTP sent to user {UserId}", user.UserId);

        return Ok(ApiResponse<SendOtpResponse>.SuccessResponse(new SendOtpResponse
        {
            Success = true,
            SessionToken = sessionToken,
            MaskedPhone = _otp.MaskPhoneNumber(user.PhoneNumber),
            Message = "تم إرسال رمز التحقق",
            ExpiresAt = DateTime.UtcNow.AddMinutes(Core.Constants.AppConstants.OtpExpirationMinutes),
            ResendCooldownSeconds = Core.Constants.AppConstants.OtpResendCooldownSeconds
        }));
    }

    // SR1.6: Reset password using OTP verification
    [AllowAnonymous]
    [HttpPost("reset-password")]
    [EnableRateLimiting("auth")]
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
