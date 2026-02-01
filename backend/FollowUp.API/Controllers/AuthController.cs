using System.Collections.Concurrent;
using System.Security.Claims;
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
    private readonly IWebHostEnvironment _env;
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
        IWebHostEnvironment env,
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
        _env = env;
        _mapper = mapper;
        _audit = audit;
    }

    // this method handle normal login with username and password
    // For Admin/Supervisor: Always requires OTP
    // For Worker: OTP only on new device
    [AllowAnonymous]
    [HttpPost("login")]
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
        var deviceId = Request.Headers["X-Device-Id"].FirstOrDefault();
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

        var response = new LoginResponse
        {
            Success = true,
            Token = token,
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

        var response = new VerifyOtpResponse
        {
            Success = true,
            Token = storedToken,
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
    /// It validates the worker's location to ensure they are physically present
    /// at their assigned work zone before allowing login.
    ///
    /// Security Features:
    /// - Device binding: Workers can only login from their registered device
    /// - Geofencing: GPS coordinates must be within assigned work zone
    /// - Location validation: GPS accuracy and bounds checking
    ///
    /// Flow:
    /// 1. Authenticate credentials (username + password)
    /// 2. Validate device ID matches registered device
    /// 3. Validate GPS accuracy and coordinates
    /// 4. Check if user is inside their assigned work zone
    /// 5. Create attendance record automatically
    /// 6. Return JWT token + attendance details
    /// </summary>
    [AllowAnonymous]
    [HttpPost("login-gps")]
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

            // Validate GPS accuracy (client-side issue - bad GPS signal quality)
            var accuracyError = ValidateGpsAccuracy(request.Accuracy);
            if (accuracyError != null)
            {
                _logger.LogWarning("GPS login rejected - Poor GPS accuracy: {Accuracy}m", request.Accuracy);
                // 400 BadRequest: GPS data quality is insufficient (client needs to improve GPS signal)
                return BadRequest(ApiResponse<LoginWithGPSResponse>.ErrorResponse(accuracyError));
            }

            // Validate GPS coordinates are not invalid (0,0 or malformed)
            var coordsError = ValidateGpsCoordinates(request.Latitude, request.Longitude);
            if (coordsError != null)
            {
                // 400 BadRequest: Invalid coordinate format
                return BadRequest(ApiResponse<LoginWithGPSResponse>.ErrorResponse(coordsError));
            }

            // Validate GPS coordinates are within municipality bounds
            var boundsError = ValidateGpsBounds(request.Latitude, request.Longitude);
            if (boundsError != null)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse(boundsError));
            }

            // Find user's zone based on GPS coordinates
            var (matchedZone, zoneError) = await FindUserZoneByGps(user.UserId, request.Latitude, request.Longitude);
            if (zoneError != null)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse(zoneError));
            }

            // token already generated during auth, use it

            // update user last login and save device registration if new
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();

            // Check if user already has attendance for today
            var existingAttendance = await _attendance.GetTodayAttendanceAsync(user.UserId);

            Attendance attendance;
            if (existingAttendance != null)
            {
                // User already checked in today - use existing attendance
                attendance = existingAttendance;
                _logger.LogInformation("User {UserId} already has attendance for today, using existing record {AttendanceId}",
                    user.UserId, attendance.AttendanceId);
            }
            else
            {
                /*
                 * ATTENDANCE RECORD CREATION
                 *
                 * Creates a new attendance record for the worker's check-in.
                 *
                 * Multi-Municipality Support:
                 * - MunicipalityId ensures data isolation between different municipalities
                 * - Each municipality can only access their own attendance records
                 * - Admins/Supervisors are scoped to their municipality
                 *
                 * Geofencing:
                 * - ZoneId links attendance to the specific work zone where user checked in
                 * - GPS coordinates are stored for audit trail and reporting
                 */
                attendance = new Attendance
                {
                    UserId = user.UserId,
                    MunicipalityId = user.MunicipalityId, // Data isolation: municipality-specific records
                    CheckInEventTime = DateTime.UtcNow,
                    CheckInLatitude = request.Latitude,
                    CheckInLongitude = request.Longitude,
                    ZoneId = matchedZone?.ZoneId, // Geofencing: which zone was the worker in?
                    IsValidated = true,
                    ValidationMessage = "تم تسجيل الحضور بنجاح داخل المنطقة المخصصة",
                    Status = Core.Enums.AttendanceStatus.CheckedIn
                };

                try
                {
                    await _attendance.AddAsync(attendance);
                    await _attendance.SaveChangesAsync();
                }
                catch (DbUpdateException)
                {
                    // Race condition: another request created attendance record between check and insert
                    // Retrieve the existing record instead
                    _logger.LogWarning("Race condition detected for user {UserId} attendance - retrieving existing record", user.UserId);
                    existingAttendance = await _attendance.GetTodayAttendanceAsync(user.UserId);
                    if (existingAttendance != null)
                    {
                        attendance = existingAttendance;
                    }
                    else
                    {
                        // This should never happen, but handle gracefully
                        throw;
                    }
                }
            }

            // build response object
            var resObj = new LoginWithGPSResponse
            {
                Success = true,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = _mapper.Map<UserDto>(user),
                Attendance = new AttendanceResponse
                {
                    AttendanceId = attendance.AttendanceId,
                    UserId = attendance.UserId,
                    UserName = user.FullName,
                    ZoneId = attendance.ZoneId,
                    ZoneName = matchedZone?.ZoneName,
                    CheckInEventTime = attendance.CheckInEventTime,
                    CheckInLatitude = attendance.CheckInLatitude,
                    CheckInLongitude = attendance.CheckInLongitude,
                    IsValidated = attendance.IsValidated,
                    ValidationMessage = attendance.ValidationMessage,
                    Status = attendance.Status,
                    CreatedAt = attendance.CheckInEventTime,
                    AttendanceType = "OnTime"
                },
                Message = "تم تسجيل الدخول والحضور بنجاح",
                CheckInStatus = "Success",
                RequiresApproval = false
            };

            return Ok(ApiResponse<LoginWithGPSResponse>.SuccessResponse(resObj, "تم تسجيل الدخول والحضور بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GPS login");
            return StatusCode(500, ApiResponse<LoginWithGPSResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

    // REMOVED: PIN login is no longer supported.
    // Workers must use username + password + GPS login instead.
    // See LoginWithGPS method for the current login implementation.

    // Helper class for check-in results
    private class CheckInResult
    {
        public bool Success { get; set; }
        public Attendance? Attendance { get; set; }
        public AttendanceResponse? AttendanceResponse { get; set; }
        public string? Error { get; set; }
        public int LateMinutes { get; set; }
        public string AttendanceType { get; set; } = "OnTime";
    }

    // Helper class for manual check-in results
    private class ManualCheckInResult
    {
        public int AttendanceId { get; set; }
        public AttendanceResponse? AttendanceResponse { get; set; }
        public int LateMinutes { get; set; }
    }

    // Try auto check-in with GPS location
    private async Task<CheckInResult> TryAutoCheckIn(User user, double latitude, double longitude, double? accuracy)
    {
        var result = new CheckInResult();

        try
        {
            // Validate GPS accuracy using helper method
            var accuracyError = ValidateGpsAccuracy(accuracy);
            if (accuracyError != null)
            {
                result.Error = accuracyError;
                return result;
            }

            // Find user's zone based on GPS coordinates using helper method
            var (matchedZone, zoneError) = await FindUserZoneByGps(user.UserId, latitude, longitude);
            if (zoneError != null)
            {
                result.Error = zoneError;
                return result;
            }

            // Calculate lateness
            var now = DateTime.UtcNow;
            var todayStart = now.Date.Add(user.ExpectedStartTime);
            var graceEnd = todayStart.AddMinutes(user.GraceMinutes);

            int lateMinutes = 0;
            string attendanceType = "OnTime";

            if (now > graceEnd)
            {
                // FIX: Calculate late minutes from grace end, not start time
                // Worker arriving within grace period should be OnTime with 0 late minutes
                lateMinutes = (int)(now - graceEnd).TotalMinutes;
                attendanceType = "Late";
            }

            // Create attendance record
            var attendance = new Attendance
            {
                UserId = user.UserId,
                MunicipalityId = user.MunicipalityId,
                CheckInEventTime = now,
                CheckInLatitude = latitude,
                CheckInLongitude = longitude,
                ZoneId = matchedZone?.ZoneId,
                IsValidated = true,
                ValidationMessage = "تم تسجيل الحضور بنجاح داخل المنطقة المخصصة",
                Status = Core.Enums.AttendanceStatus.CheckedIn,
                LateMinutes = lateMinutes,
                AttendanceType = attendanceType,
                ApprovalStatus = "AutoApproved",
                IsManual = false
            };

            await _attendance.AddAsync(attendance);
            await _attendance.SaveChangesAsync();

            result.Success = true;
            result.Attendance = attendance;
            result.LateMinutes = lateMinutes;
            result.AttendanceType = attendanceType;
            result.AttendanceResponse = new AttendanceResponse
            {
                AttendanceId = attendance.AttendanceId,
                UserId = attendance.UserId,
                UserName = user.FullName,
                ZoneId = attendance.ZoneId,
                ZoneName = matchedZone?.ZoneName,
                CheckInEventTime = attendance.CheckInEventTime,
                CheckInLatitude = attendance.CheckInLatitude,
                CheckInLongitude = attendance.CheckInLongitude,
                IsValidated = attendance.IsValidated,
                ValidationMessage = attendance.ValidationMessage,
                Status = attendance.Status,
                CreatedAt = attendance.CheckInEventTime,
                LateMinutes = lateMinutes,
                AttendanceType = attendanceType,
                ApprovalStatus = "AutoApproved"
            };

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto check-in for user {UserId}", user.UserId);
            result.Error = "حدث خطأ أثناء تسجيل الحضور";
            return result;
        }
    }

    // Create manual check-in (GPS failure fallback) - requires supervisor approval
    private async Task<ManualCheckInResult> CreateManualCheckIn(User user, string reason)
    {
        // Calculate lateness
        var now = DateTime.UtcNow;
        var todayStart = now.Date.Add(user.ExpectedStartTime);
        var graceEnd = todayStart.AddMinutes(user.GraceMinutes);

        int lateMinutes = 0;
        if (now > graceEnd)
        {
            // FIX: Calculate late minutes from grace end, not start time
            // Consistent with auto check-in calculation
            lateMinutes = (int)(now - graceEnd).TotalMinutes;
        }

        var attendance = new Attendance
        {
            UserId = user.UserId,
            MunicipalityId = user.MunicipalityId,
            CheckInEventTime = now,
            CheckInLatitude = 0, // No GPS
            CheckInLongitude = 0,
            ZoneId = null,
            IsValidated = false, // Needs supervisor validation
            ValidationMessage = "حضور يدوي - بانتظار موافقة المشرف",
            Status = Core.Enums.AttendanceStatus.CheckedIn,
            LateMinutes = lateMinutes,
            AttendanceType = "Manual",
            ApprovalStatus = "Pending",
            IsManual = true,
            ManualReason = reason
        };

        await _attendance.AddAsync(attendance);
        await _attendance.SaveChangesAsync();

        return new ManualCheckInResult
        {
            AttendanceId = attendance.AttendanceId,
            LateMinutes = lateMinutes,
            AttendanceResponse = new AttendanceResponse
            {
                AttendanceId = attendance.AttendanceId,
                UserId = attendance.UserId,
                UserName = user.FullName,
                CheckInEventTime = attendance.CheckInEventTime,
                IsValidated = false,
                ValidationMessage = attendance.ValidationMessage,
                Status = attendance.Status,
                CreatedAt = attendance.CheckInEventTime,
                LateMinutes = lateMinutes,
                AttendanceType = "Manual",
                ApprovalStatus = "Pending",
                IsManual = true,
                ManualReason = reason
            }
        };
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
    /// Validates GPS coordinates are not invalid (0,0)
    /// </summary>
    private string? ValidateGpsCoordinates(double latitude, double longitude)
    {
        if (latitude == 0 && longitude == 0)
        {
            return "إحداثيات GPS غير صالحة. يرجى تفعيل GPS.";
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
            FullName = request.FullName,
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

        var userDto = _mapper.Map<UserDto>(createdUser!);
        return CreatedAtAction(nameof(GetProfile), null, ApiResponse<UserDto>.SuccessResponse(userDto, "تم تسجيل المستخدم بنجاح"));
    }

    // get current user profile (alias: /me for frontend compatibility)
    [HttpGet("me")]
    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            return Unauthorized(ApiResponse<UserDto>.ErrorResponse("رمز دخول غير صالح"));

        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.ErrorResponse("المستخدم غير موجود"));

        var userDto = _mapper.Map<UserDto>(user);
        return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
    }

    // logout the user
    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdClaim, out var userId))
        {
            await _auth.LogoutAsync(userId);
        }

        return Ok(ApiResponse<string>.SuccessResponse("تم تسجيل الخروج بنجاح"));
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

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز مستخدم غير صالح"));

        var user = await _users.GetByIdAsync(userId);
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
