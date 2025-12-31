using System.Security.Claims;
using AutoMapper;
using Jawlah.Core.DTOs.Auth;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
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

    public AuthController(
        IAuthService auth,
        IUserRepository users,
        IZoneRepository zones,
        IAttendanceRepository attendance,
        ILogger<AuthController> logger,
        IConfiguration config,
        IWebHostEnvironment env,
        IMapper mapper)
    {
        _auth = auth;
        _users = users;
        _zones = zones;
        _attendance = attendance;
        _logger = logger;
        _config = config;
        _env = env;
        _mapper = mapper;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse("طلب غير صالح"));
            }

            var (success, token, refreshToken, error) = await _auth.LoginAsync(request.Username, request.Password);

            if (!success)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تسجيل الدخول"));
            }

            var user = await _users.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResponse("المستخدم غير موجود"));
            }

            var expirationMinutes = int.Parse(_config["JwtSettings:ExpirationMinutes"] ?? "1440");

            var response = new LoginResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                User = _mapper.Map<UserDto>(user)
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "تم تسجيل الدخول بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

    [AllowAnonymous]
    [HttpPost("login-gps")]
    public async Task<IActionResult> LoginWithGPS([FromBody] LoginWithGPSRequest request)
    {
        try
        {
            // check if the request data is correct
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

            // 1. find user by his PIN
            var user = await _users.GetByPinAsync(request.Pin);

            if (user == null)
            {
                _logger.LogWarning("GPS login failed - Invalid PIN");
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("الرقم السري غير صحيح"));
            }

            if (user.Status != Core.Enums.UserStatus.Active)
            {
                _logger.LogWarning("GPS login failed - User {UserId} is not active", user.UserId);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("حساب المستخدم غير نشط"));
            }

            // 2. check GPS location
            // check if accuracy is good enough
            if (request.Accuracy.HasValue && request.Accuracy.Value > Core.Constants.GeofencingConstants.MaxAcceptableAccuracyMeters)
            {
                _logger.LogWarning("GPS login rejected - Poor GPS accuracy: {Accuracy}m", request.Accuracy.Value);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse($"دقة GPS منخفضة جداً ({request.Accuracy:F1}م). يرجى المحاولة في مكان أفضل."));
            }

            // check if lat and long are zeros
            if (request.Latitude == 0 && request.Longitude == 0)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("إحداثيات GPS غير صالحة. يرجى تفعيل GPS."));
            }

            // validate coordinates are inside Palestine area
            if (request.Latitude < Core.Constants.GeofencingConstants.MinLatitude ||
                request.Latitude > Core.Constants.GeofencingConstants.MaxLatitude ||
                request.Longitude < Core.Constants.GeofencingConstants.MinLongitude ||
                request.Longitude > Core.Constants.GeofencingConstants.MaxLongitude)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("الموقع خارج منطقة الخدمة. يرجى التحقق من GPS."));
            }

            Zone? matchedZone = null;
            bool isInsideZone = false;

            // check developer mode bypass for testing
            var disableGeofencing = _config.GetValue<bool>("DeveloperMode:DisableGeofencing", false);
            if (disableGeofencing && _env.IsDevelopment())
            {
                _logger.LogWarning("⚠️ SECURITY: Geofencing validation BYPASSED - Development mode only.");
                var userWithZonesBypass = await _users.GetUserWithZonesAsync(user.UserId);
                var assignedZoneIdsBypass = userWithZonesBypass?.AssignedZones?.Where(uz => uz.IsActive).Select(uz => uz.ZoneId).ToList() ?? new List<int>();

                if (assignedZoneIdsBypass.Any())
                {
                    matchedZone = await _zones.GetByIdAsync(assignedZoneIdsBypass.First());
                    isInsideZone = true;
                }
                else
                {
                    isInsideZone = true;
                }
            }
            else
            {
                // load user zones and check if inside any of them
                var userWithZones = await _users.GetUserWithZonesAsync(user.UserId);
                var assignedZoneIds = userWithZones?.AssignedZones?.Where(uz => uz.IsActive).Select(uz => uz.ZoneId).ToList() ?? new List<int>();

                if (!assignedZoneIds.Any())
                {
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("لا يوجد مناطق عمل مخصصة لهذا المستخدم."));
                }

                var assignedZones = (await _zones.GetZonesByIdsAsync(assignedZoneIds)).Where(z => z.IsActive).ToList();
                var point = NetTopologySuite.Geometries.GeometryFactory.Default.CreatePoint(new NetTopologySuite.Geometries.Coordinate(request.Longitude, request.Latitude));

                foreach (var z in assignedZones)
                {
                    if (z.Boundary == null) continue;

                    // check if inside or close to boundary
                    if (z.Boundary.Contains(point) || z.Boundary.Distance(point) <= Core.Constants.GeofencingConstants.BufferToleranceDegrees)
                    {
                        matchedZone = z;
                        isInsideZone = true;
                        break;
                    }
                }
            }

            if (!isInsideZone)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse("أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور."));
            }

            // 3. create login token
            var (success, token, refreshToken, tokenError) = await _auth.GenerateTokenForUserAsync(user);
            if (!success)
            {
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResponse(tokenError ?? "فشل تسجيل الدخول"));
            }

            // 4. save attendance record in database
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

            // 5. prepare final response
            var expirationMinutes = int.Parse(_config["JwtSettings:ExpirationMinutes"] ?? "1440");
            var resObj = new LoginWithGPSResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
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

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<UserDto>.ErrorResponse(errors));
            }

            if (!Utils.InputSanitizer.IsStrongPassword(request.Password))
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse("كلمة المرور يجب أن تكون 8 أحرف على الأقل وتحتوي على حرف ورقم"));
            }

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
                    {
                        return BadRequest(ApiResponse<UserDto>.ErrorResponse("الرقم السري موجود مسبقاً، يرجى استخدام رقم آخر"));
                    }
                }
            }

            var user = new User
            {
                Username = request.Username,
                FullName = request.FullName,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Role = request.Role,
                WorkerType = request.WorkerType,
                Department = request.Department,
                Pin = request.Role == Core.Enums.UserRole.Worker ? workerPin : null
            };

            var (success, createdUser, error) = await _auth.RegisterAsync(user, request.Password);

            if (!success)
            {
                return BadRequest(ApiResponse<UserDto>.ErrorResponse(error ?? "فشل التسجيل"));
            }

            var userDto = _mapper.Map<UserDto>(createdUser!);

            return CreatedAtAction(nameof(GetProfile), null, ApiResponse<UserDto>.SuccessResponse(userDto, "تم تسجيل المستخدم بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration");
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("حدث خطأ أثناء التسجيل"));
        }
    }

    [HttpGet("profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<UserDto>.ErrorResponse("رمز دخول غير صالح"));
            }

            var user = await _users.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResponse("المستخدم غير موجود"));
            }

            var userDto = _mapper.Map<UserDto>(user);

            return Ok(ApiResponse<UserDto>.SuccessResponse(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, ApiResponse<UserDto>.ErrorResponse("حدث خطأ أثناء جلب الملف الشخصي"));
        }
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        try
        {

            var (success, newToken, error) = await _auth.RefreshTokenAsync(refreshToken);

            if (!success)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResponse(error ?? "فشل تحديث الرمز"));
            }

            var response = new LoginResponse
            {
                Success = true,
                Token = newToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["JwtSettings:ExpirationMinutes"] ?? "1440"))
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResponse(response, "تم تحديث الرمز بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResponse("حدث خطأ أثناء تحديث الرمز"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                await _auth.LogoutAsync(userId);
            }

            return Ok(ApiResponse<string>.SuccessResponse("تم تسجيل الخروج بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<string>.ErrorResponse("حدث خطأ أثناء تسجيل الخروج"));
        }
    }

    [HttpPost("register-fcm-token")]
    [Authorize]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("طلب غير صالح"));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResponse("رمز مستخدم غير صالح"));
            }

            var user = await _users.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));
            }

            user.FcmToken = request.FcmToken;
            await _users.UpdateAsync(user);
            await _users.SaveChangesAsync();

            return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تسجيل رمز الإشعارات بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering FCM token");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("حدث خطأ أثناء تسجيل رمز الإشعارات"));
        }
    }

    private static readonly Random _randomGenerator = Random.Shared;

    private async Task<string> makeNewPin()
    {
        string pin;
        bool isUnique;

        do
        {
            pin = _randomGenerator.Next(1000, 9999).ToString();
            isUnique = await _users.IsPinUniqueAsync(pin);
        } while (!isUnique);

        return pin;
    }
}
