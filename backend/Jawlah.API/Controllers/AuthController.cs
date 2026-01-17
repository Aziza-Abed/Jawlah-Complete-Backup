using System.Collections.Concurrent;
using System.Security.Claims;
using AutoMapper;
using Jawlah.Core.DTOs.Auth;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class AuthController : BaseApiController
{
    private readonly IAuthService _auth;
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly IAttendanceRepository _attendance;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _config;
    private readonly IWebHostEnvironment _env;
    private readonly IMapper _mapper;
    private readonly AuditLogService _audit;

    // thread-safe login attempt tracking to prevent brute force
    private static readonly ConcurrentDictionary<string, (int FailedCount, DateTime? LockoutUntil)> _loginAttempts = new();
    private const int MaxFailedAttempts = 5;
    private const int LockoutMinutes = 15;
    private static DateTime _lastCleanup = DateTime.UtcNow;

    public AuthController(
        IAuthService auth,
        IUserRepository users,
        IZoneRepository zones,
        IAttendanceRepository attendance,
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
        _logger = logger;
        _config = config;
        _env = env;
        _mapper = mapper;
        _audit = audit;
    }

    // this method handle normal login with username and password
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("طلب غير صالح"));

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var userAgent = Request.Headers.UserAgent.ToString();

        var (success, token, refreshToken, error) = await _auth.LoginAsync(request.Username, request.Password);
        if (!success)
        {
            // UR23: Log failed login
            await _audit.LogAsync(null, request.Username, "LoginFailed", error, ipAddress, userAgent);
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تسجيل الدخول"));
        }

        var user = await _users.GetByUsernameAsync(request.Username);
        if (user == null)
            return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("المستخدم غير موجود"));

        // UR23: Log successful login
        await _audit.LogAsync(user.UserId, user.Username, "Login", "تسجيل دخول ناجح", ipAddress, userAgent);

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

    // this method handle login with gps location for workers
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

            // validate PIN format first
            if (string.IsNullOrWhiteSpace(request.Pin) || request.Pin.Length != 4 || !request.Pin.All(char.IsDigit))
            {
                return BadRequest(ApiResponse<LoginWithGPSResponse>.ErrorResponse("الرقم السري يجب أن يكون 4 أرقام"));
            }

            // check if PIN is locked out
            if (IsLockedOut(request.Pin, out var remainingMinutes))
            {
                _logger.LogWarning("GPS login blocked - PIN is locked out for {Minutes} more minutes", remainingMinutes);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse($"تم حظر الرقم السري مؤقتاً. حاول مرة أخرى بعد {remainingMinutes} دقيقة."));
            }

            // find user by pin code
            var user = await _users.GetByPinAsync(request.Pin);

            if (user == null)
            {
                RecordFailedAttempt(request.Pin);
                var attemptsLeft = MaxFailedAttempts - GetFailedAttemptCount(request.Pin);
                _logger.LogWarning("GPS login failed - Invalid PIN. Attempts left: {AttemptsLeft}", attemptsLeft);

                if (attemptsLeft <= 0)
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse($"تم حظر الرقم السري لمدة {LockoutMinutes} دقيقة بسبب كثرة المحاولات الخاطئة."));

                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse($"الرقم السري غير صحيح. ({attemptsLeft} محاولات متبقية)"));
            }

            // check if user is active or not
            if (user.Status != Core.Enums.UserStatus.Active)
            {
                _logger.LogWarning("GPS login failed - User {UserId} is not active", user.UserId);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("حساب المستخدم غير نشط"));
            }

            // device binding security check - prevents PIN theft
            var disableDeviceBinding = _config.GetValue<bool>("DeveloperMode:DisableDeviceBinding");
            if (disableDeviceBinding)
            {
                _logger.LogWarning("SECURITY: Device binding is DISABLED. Set DeveloperMode:DisableDeviceBinding=false for production!");
            }
            else if (!string.IsNullOrEmpty(request.DeviceId))
            {
                if (string.IsNullOrEmpty(user.RegisteredDeviceId))
                {
                    // first login - register this device
                    user.RegisteredDeviceId = request.DeviceId;
                    _logger.LogInformation("Device registered for user {UserId}: {DeviceId}", user.UserId, request.DeviceId);
                }
                else if (user.RegisteredDeviceId != request.DeviceId)
                {
                    // different device trying to login - reject for security
                    _logger.LogWarning("Device binding failed - User {UserId} attempted login from unregistered device. Expected: {Expected}, Got: {Got}",
                        user.UserId, user.RegisteredDeviceId, request.DeviceId);
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("هذا الجهاز غير مسجل لهذا الحساب. يرجى التواصل مع المشرف."));
                }
            }

            // validate gps accuracy is good enought
            if (request.Accuracy.HasValue && request.Accuracy.Value > Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters)
            {
                _logger.LogWarning("GPS login rejected - Poor GPS accuracy: {Accuracy}m", request.Accuracy.Value);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse($"دقة GPS منخفضة جداً ({request.Accuracy.Value:F1}م). يرجى المحاولة في مكان أفضل."));
            }

            // check if coords are zeros which is bad
            if (request.Latitude == 0 && request.Longitude == 0)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("إحداثيات GPS غير صالحة. يرجى تفعيل GPS."));
            }

            // check if demo/testing mode is enabled for university testing
            var isTestingMode = _config.GetValue<bool>("DeveloperMode:DisableGeofencing");

            // validate coords are inside municipality area (or extended testing area)
            double minLat, maxLat, minLon, maxLon;
            if (isTestingMode)
            {
                // use extended bounds for testing (includes Birzeit University)
                minLat = Core.Constants.GeofencingConstants.TestingMinLatitude;
                maxLat = Core.Constants.GeofencingConstants.TestingMaxLatitude;
                minLon = Core.Constants.GeofencingConstants.TestingMinLongitude;
                maxLon = Core.Constants.GeofencingConstants.TestingMaxLongitude;
            }
            else
            {
                // production bounds - Al-Bireh municipality only
                minLat = Core.Constants.GeofencingConstants.MinLatitude;
                maxLat = Core.Constants.GeofencingConstants.MaxLatitude;
                minLon = Core.Constants.GeofencingConstants.MinLongitude;
                maxLon = Core.Constants.GeofencingConstants.MaxLongitude;
            }

            if (request.Latitude < minLat || request.Latitude > maxLat ||
                request.Longitude < minLon || request.Longitude > maxLon)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("الموقع خارج منطقة الخدمة. يرجى التحقق من GPS."));
            }

            // get user zones from db
            var userWithZones = await _users.GetUserWithZonesAsync(user.UserId);
            var assignedZoneIds = userWithZones?.AssignedZones?.Where(uz => uz.IsActive).Select(uz => uz.ZoneId).ToList() ?? new List<int>();

            Zone? matchedZone = null;
            bool isInsideZone = false;

            // check if geofencing is disabled for development
            var disableGeofencing = _config.GetValue<bool>("DeveloperMode:DisableGeofencing");

            if (disableGeofencing)
            {
                // skip zone validation in dev mode, just get first assigned zone
                if (assignedZoneIds.Any())
                {
                    var assignedZones = (await _zones.GetZonesByIdsAsync(assignedZoneIds)).Where(z => z.IsActive).ToList();
                    matchedZone = assignedZones.FirstOrDefault();
                }
                isInsideZone = true;
            }
            else
            {
                // check if user has assigned zones
                if (!assignedZoneIds.Any())
                {
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("لا يوجد مناطق عمل مخصصة لهذا المستخدم."));
                }

                // validate user is inside their assigned zone
                var assignedZones = (await _zones.GetZonesByIdsAsync(assignedZoneIds)).Where(z => z.IsActive).ToList();
                var point = NetTopologySuite.Geometries.GeometryFactory.Default.CreatePoint(new NetTopologySuite.Geometries.Coordinate(request.Longitude, request.Latitude));

                foreach (var z in assignedZones)
                {
                    if (z.Boundary == null) continue;

                    if (z.Boundary.Contains(point) || z.Boundary.Distance(point) <= Core.Constants.GeofencingConstants.BufferToleranceDegrees)
                    {
                        matchedZone = z;
                        isInsideZone = true;
                        break;
                    }
                }

                // user not inside any zone
                if (!isInsideZone)
                {
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور."));
                }
            }

            // generate jwt token for the user
            var (success, token, refreshToken, tokenError) = await _auth.GenerateTokenForUserAsync(user);
            if (!success)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse(tokenError ?? "فشل تسجيل الدخول"));
            }

            // reset failed attempts on successful login
            ResetFailedAttempts(request.Pin);

            // update user last login and save device registration if new
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();

            // create attendance record in db
            var attendance = new Attendance
            {
                UserId = user.UserId,
                CheckInEventTime = DateTime.UtcNow,
                CheckInLatitude = request.Latitude,
                CheckInLongitude = request.Longitude,
                ZoneId = matchedZone?.ZoneId,
                IsValidated = true,
                ValidationMessage = "تم تسجيل الحضور بنجاح داخل المنطقة المخصصة",
                Status = Core.Enums.AttendanceStatus.CheckedIn
            };

            await _attendance.AddAsync(attendance);
            await _attendance.SaveChangesAsync();

            // build response object
            var resObj = new LoginWithGPSResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
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
                    CreatedAt = attendance.CheckInEventTime
                },
                Message = "تم تسجيل الدخول والحضور بنجاح"
            };

            return Ok(ApiResponse<LoginWithGPSResponse>.SuccessResponse(resObj, "تم تسجيل الدخول والحضور بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GPS login");
            return StatusCode(500, ApiResponse<LoginWithGPSResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

    // this method handles PIN login with optional auto check-in
    // If location is provided and valid, worker is automatically checked in
    // GPS failure fallback: If AllowManualCheckIn=true and ManualReason provided, creates pending attendance
    [AllowAnonymous]
    [HttpPost("login-pin")]
    public async Task<IActionResult> LoginWithPin([FromBody] LoginWithPinRequest request)
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
                return BadRequest(ApiResponse<LoginWithPinResponse>.ErrorResponse(errors));
            }

            // validate PIN format first
            if (string.IsNullOrWhiteSpace(request.Pin) || request.Pin.Length != 4 || !request.Pin.All(char.IsDigit))
            {
                return BadRequest(ApiResponse<LoginWithPinResponse>.ErrorResponse("الرقم السري يجب أن يكون 4 أرقام"));
            }

            // check if PIN is locked out
            if (IsLockedOut(request.Pin, out var remainingMinutes))
            {
                _logger.LogWarning("PIN login blocked - PIN is locked out for {Minutes} more minutes", remainingMinutes);
                return Unauthorized(ApiResponse<LoginWithPinResponse>.ErrorResponse($"تم حظر الرقم السري مؤقتاً. حاول مرة أخرى بعد {remainingMinutes} دقيقة."));
            }

            // find user by pin code
            var user = await _users.GetByPinAsync(request.Pin);

            if (user == null)
            {
                RecordFailedAttempt(request.Pin);
                var attemptsLeft = MaxFailedAttempts - GetFailedAttemptCount(request.Pin);
                _logger.LogWarning("PIN login failed - Invalid PIN. Attempts left: {AttemptsLeft}", attemptsLeft);

                if (attemptsLeft <= 0)
                    return Unauthorized(ApiResponse<LoginWithPinResponse>.ErrorResponse($"تم حظر الرقم السري لمدة {LockoutMinutes} دقيقة بسبب كثرة المحاولات الخاطئة."));

                return Unauthorized(ApiResponse<LoginWithPinResponse>.ErrorResponse($"الرقم السري غير صحيح. ({attemptsLeft} محاولات متبقية)"));
            }

            // check if user is active or not
            if (user.Status != Core.Enums.UserStatus.Active)
            {
                _logger.LogWarning("PIN login failed - User {UserId} is not active", user.UserId);
                return Unauthorized(ApiResponse<LoginWithPinResponse>.ErrorResponse("حساب المستخدم غير نشط"));
            }

            // device binding security check - prevents PIN theft
            var disableDeviceBinding = _config.GetValue<bool>("DeveloperMode:DisableDeviceBinding");
            if (disableDeviceBinding)
            {
                _logger.LogWarning("SECURITY: Device binding is DISABLED. Set DeveloperMode:DisableDeviceBinding=false for production!");
            }
            else if (!string.IsNullOrEmpty(request.DeviceId))
            {
                if (string.IsNullOrEmpty(user.RegisteredDeviceId))
                {
                    // first login - register this device
                    user.RegisteredDeviceId = request.DeviceId;
                    _logger.LogInformation("Device registered for user {UserId}: {DeviceId}", user.UserId, request.DeviceId);
                }
                else if (user.RegisteredDeviceId != request.DeviceId)
                {
                    // different device trying to login - reject for security
                    _logger.LogWarning("Device binding failed - User {UserId} attempted login from unregistered device. Expected: {Expected}, Got: {Got}",
                        user.UserId, user.RegisteredDeviceId, request.DeviceId);
                    return Unauthorized(ApiResponse<LoginWithPinResponse>.ErrorResponse("هذا الجهاز غير مسجل لهذا الحساب. يرجى التواصل مع المشرف."));
                }
            }

            // generate jwt token for the user
            var (success, token, refreshToken, tokenError) = await _auth.GenerateTokenForUserAsync(user);
            if (!success)
            {
                return Unauthorized(ApiResponse<LoginWithPinResponse>.ErrorResponse(tokenError ?? "فشل تسجيل الدخول"));
            }

            // reset failed attempts on successful login
            ResetFailedAttempts(request.Pin);

            // update user last login and save device registration if new
            user.LastLoginAt = DateTime.UtcNow;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();

            // check if worker already has an active attendance (checked in today)
            var activeAttendance = await _attendance.GetActiveAttendanceAsync(user.UserId);

            // build response object
            var resObj = new LoginWithPinResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes()),
                User = _mapper.Map<UserDto>(user),
                CheckInStatus = "NotAttempted"
            };

            // If already checked in, return existing attendance
            if (activeAttendance != null)
            {
                resObj.IsCheckedIn = true;
                resObj.ActiveAttendanceId = activeAttendance.AttendanceId;
                resObj.CheckInStatus = "Success";
                resObj.LateMinutes = activeAttendance.LateMinutes;
                resObj.IsLate = activeAttendance.LateMinutes > 0;
                resObj.AttendanceType = activeAttendance.AttendanceType;
                resObj.Message = "تم تسجيل الدخول بنجاح - أنت مسجل حضورك";
                return Ok(ApiResponse<LoginWithPinResponse>.SuccessResponse(resObj, resObj.Message));
            }

            // Try auto check-in if location is provided
            bool locationProvided = request.Latitude.HasValue && request.Longitude.HasValue &&
                                    !(request.Latitude == 0 && request.Longitude == 0);

            if (locationProvided)
            {
                // Attempt GPS-based check-in
                var checkInResult = await TryAutoCheckIn(user, request.Latitude!.Value, request.Longitude!.Value, request.Accuracy);
                if (checkInResult.Success)
                {
                    resObj.IsCheckedIn = true;
                    resObj.ActiveAttendanceId = checkInResult.Attendance!.AttendanceId;
                    resObj.Attendance = checkInResult.AttendanceResponse;
                    resObj.CheckInStatus = "Success";
                    resObj.LateMinutes = checkInResult.LateMinutes;
                    resObj.IsLate = checkInResult.LateMinutes > 0;
                    resObj.AttendanceType = checkInResult.AttendanceType;
                    resObj.Message = checkInResult.LateMinutes > 0
                        ? $"تم تسجيل الدخول والحضور بنجاح (متأخر {checkInResult.LateMinutes} دقيقة)"
                        : "تم تسجيل الدخول والحضور بنجاح";
                    return Ok(ApiResponse<LoginWithPinResponse>.SuccessResponse(resObj, resObj.Message));
                }
                else
                {
                    // GPS check-in failed (outside zone, etc.)
                    resObj.CheckInStatus = "Failed";
                    resObj.CheckInFailureReason = checkInResult.Error;
                }
            }

            // GPS failed or not provided - check if manual check-in requested
            if (request.AllowManualCheckIn && !string.IsNullOrWhiteSpace(request.ManualCheckInReason))
            {
                var manualResult = await CreateManualCheckIn(user, request.ManualCheckInReason);
                resObj.IsCheckedIn = true;
                resObj.ActiveAttendanceId = manualResult.AttendanceId;
                resObj.Attendance = manualResult.AttendanceResponse;
                resObj.CheckInStatus = "PendingApproval";
                resObj.RequiresApproval = true;
                resObj.LateMinutes = manualResult.LateMinutes;
                resObj.IsLate = manualResult.LateMinutes > 0;
                resObj.AttendanceType = "Manual";
                resObj.Message = "تم تسجيل الدخول بنجاح - الحضور بانتظار موافقة المشرف";
                return Ok(ApiResponse<LoginWithPinResponse>.SuccessResponse(resObj, resObj.Message));
            }

            // No check-in attempted or failed
            resObj.IsCheckedIn = false;
            resObj.Message = locationProvided
                ? $"تم تسجيل الدخول - فشل تسجيل الحضور: {resObj.CheckInFailureReason}"
                : "تم تسجيل الدخول بنجاح - يرجى تسجيل الحضور للبدء بالعمل";

            return Ok(ApiResponse<LoginWithPinResponse>.SuccessResponse(resObj, resObj.Message));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during PIN login");
            return StatusCode(500, ApiResponse<LoginWithPinResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

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
            // Validate GPS accuracy
            if (accuracy.HasValue && accuracy.Value > Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters)
            {
                result.Error = $"دقة GPS منخفضة ({accuracy.Value:F1}م)";
                return result;
            }

            // Check if geofencing is disabled for development
            var disableGeofencing = _config.GetValue<bool>("DeveloperMode:DisableGeofencing");

            Zone? matchedZone = null;

            if (disableGeofencing)
            {
                // Skip zone validation in dev mode
                var userWithZones = await _users.GetUserWithZonesAsync(user.UserId);
                var assignedZoneIds = userWithZones?.AssignedZones?.Where(uz => uz.IsActive).Select(uz => uz.ZoneId).ToList() ?? new List<int>();
                if (assignedZoneIds.Any())
                {
                    var assignedZones = (await _zones.GetZonesByIdsAsync(assignedZoneIds)).Where(z => z.IsActive).ToList();
                    matchedZone = assignedZones.FirstOrDefault();
                }
            }
            else
            {
                // Validate user is inside their assigned zone
                var userWithZones = await _users.GetUserWithZonesAsync(user.UserId);
                var assignedZoneIds = userWithZones?.AssignedZones?.Where(uz => uz.IsActive).Select(uz => uz.ZoneId).ToList() ?? new List<int>();

                if (!assignedZoneIds.Any())
                {
                    result.Error = "لا يوجد مناطق عمل مخصصة";
                    return result;
                }

                var assignedZones = (await _zones.GetZonesByIdsAsync(assignedZoneIds)).Where(z => z.IsActive).ToList();
                var point = NetTopologySuite.Geometries.GeometryFactory.Default.CreatePoint(new NetTopologySuite.Geometries.Coordinate(longitude, latitude));

                foreach (var z in assignedZones)
                {
                    if (z.Boundary == null) continue;
                    if (z.Boundary.Contains(point) || z.Boundary.Distance(point) <= Core.Constants.GeofencingConstants.BufferToleranceDegrees)
                    {
                        matchedZone = z;
                        break;
                    }
                }

                if (matchedZone == null)
                {
                    result.Error = "أنت خارج منطقة العمل المخصصة";
                    return result;
                }
            }

            // Calculate lateness
            var now = DateTime.UtcNow;
            var todayStart = now.Date.Add(user.ExpectedStartTime);
            var graceEnd = todayStart.AddMinutes(user.GraceMinutes);

            int lateMinutes = 0;
            string attendanceType = "OnTime";

            if (now > graceEnd)
            {
                lateMinutes = (int)(now - todayStart).TotalMinutes;
                attendanceType = "Late";
            }

            // Create attendance record
            var attendance = new Attendance
            {
                UserId = user.UserId,
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
            lateMinutes = (int)(now - todayStart).TotalMinutes;
        }

        var attendance = new Attendance
        {
            UserId = user.UserId,
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

    // register new user only admin can do this
    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
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

        // generate pin for workers
        string? workerPin = request.Pin;
        if (request.Role == Core.Enums.UserRole.Worker)
        {
            if (string.IsNullOrEmpty(workerPin))
            {
                workerPin = await makeNewPin();
            }
            else
            {
                var isPinUnique = await _users.IsPinUniqueAsync(workerPin);
                if (!isPinUnique)
                    return BadRequest(ApiResponse<UserDto>.ErrorResponse("الرقم السري موجود مسبقاً، يرجى استخدام رقم آخر"));
            }
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
            Department = request.Department,
            Pin = request.Role == Core.Enums.UserRole.Worker ? workerPin : null
        };

        var (success, createdUser, error) = await _auth.RegisterAsync(user, request.Password);
        if (!success)
            return BadRequest(ApiResponse<UserDto>.ErrorResponse(error ?? "فشل التسجيل"));

        var userDto = _mapper.Map<UserDto>(createdUser!);
        return CreatedAtAction(nameof(GetProfile), null, ApiResponse<UserDto>.SuccessResponse(userDto, "تم تسجيل المستخدم بنجاح"));
    }

    // get current user profile
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

    // refresh the jwt token - requires valid (even expired) JWT to prevent token theft
    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        var (success, newToken, error) = await _auth.RefreshTokenAsync(refreshToken);
        if (!success)
            return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تحديث الرمز"));

        var response = new LoginResponse
        {
            Success = true,
            Token = newToken,
            ExpiresAt = DateTime.UtcNow.AddMinutes(GetTokenExpirationMinutes())
        };

        return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "تم تحديث الرمز بنجاح"));
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

    // Record user's privacy consent
    [HttpPost("consent")]
    [Authorize]
    public async Task<IActionResult> RecordPrivacyConsent()
    {
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز مستخدم غير صالح"));

        var user = await _users.GetByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // Record consent with current timestamp and version
        user.PrivacyConsentedAt = DateTime.UtcNow;
        user.ConsentVersion = UserDto.RequiredConsentVersion;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("Privacy consent recorded for user {UserId} at {Time}", userId, user.PrivacyConsentedAt);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تسجيل الموافقة على سياسة الخصوصية"));
    }

    // save fcm token for push notifications
    [HttpPost("register-fcm-token")]
    [Authorize]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("طلب غير صالح"));

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

    // generate random 4 digit pin for workers
    private async Task<string> makeNewPin()
    {
        const int maxAttempts = 100;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            var pin = Random.Shared.Next(1000, 9999).ToString();
            if (await _users.IsPinUniqueAsync(pin))
                return pin;
            attempts++;
        }

        throw new InvalidOperationException("Unable to generate unique PIN after maximum attempts");
    }

    // check if PIN is currently locked out
    private static bool IsLockedOut(string pin, out int remainingMinutes)
    {
        remainingMinutes = 0;

        // periodic cleanup of expired entries (every 30 minutes)
        if ((DateTime.UtcNow - _lastCleanup).TotalMinutes > 30)
        {
            CleanupExpiredAttempts();
            _lastCleanup = DateTime.UtcNow;
        }

        if (_loginAttempts.TryGetValue(pin, out var attempt))
        {
            if (attempt.LockoutUntil.HasValue && DateTime.UtcNow < attempt.LockoutUntil.Value)
            {
                remainingMinutes = (int)Math.Ceiling((attempt.LockoutUntil.Value - DateTime.UtcNow).TotalMinutes);
                return true;
            }
            // lockout expired, reset
            _loginAttempts.TryRemove(pin, out _);
        }
        return false;
    }

    // record a failed login attempt
    private static void RecordFailedAttempt(string pin)
    {
        _loginAttempts.AddOrUpdate(
            pin,
            _ => (1, null),
            (_, current) =>
            {
                var newCount = current.FailedCount + 1;
                return newCount >= MaxFailedAttempts
                    ? (newCount, DateTime.UtcNow.AddMinutes(LockoutMinutes))
                    : (newCount, null);
            });
    }

    // get current failed attempt count
    private static int GetFailedAttemptCount(string pin)
    {
        return _loginAttempts.TryGetValue(pin, out var attempt) ? attempt.FailedCount : 0;
    }

    // reset failed attempts after successful login
    private static void ResetFailedAttempts(string pin)
    {
        _loginAttempts.TryRemove(pin, out _);
    }

    // cleanup expired lockout entries to prevent memory leak
    private static void CleanupExpiredAttempts()
    {
        var expiredKeys = _loginAttempts
            .Where(kvp => kvp.Value.LockoutUntil.HasValue && kvp.Value.LockoutUntil.Value < DateTime.UtcNow)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            _loginAttempts.TryRemove(key, out _);
        }
    }
}
