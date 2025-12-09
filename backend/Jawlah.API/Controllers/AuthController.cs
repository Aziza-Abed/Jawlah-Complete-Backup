using System.Security.Claims;
using Jawlah.Core.DTOs.Auth;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly IUserRepository _userRepo;
    private readonly IZoneRepository _zoneRepo;
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<AuthController> _logger;
    private readonly IConfiguration _configuration;

    public AuthController(
        IAuthService authService,
        IUserRepository userRepo,
        IZoneRepository zoneRepo,
        IAttendanceRepository attendanceRepo,
        JawlahDbContext context,
        ILogger<AuthController> logger,
        IConfiguration configuration)
    {
        _authService = authService;
        _userRepo = userRepo;
        _zoneRepo = zoneRepo;
        _attendanceRepo = attendanceRepo;
        _context = context;
        _logger = logger;
        _configuration = configuration;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            _logger.LogInformation("Login attempt for username: {Username}", request.Username);

            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult("Invalid request"));
            }

            var (success, token, refreshToken, error) = await _authService.LoginAsync(request.Username, request.Password);

            if (!success)
            {
                _logger.LogWarning("Login failed for username: {Username}. Error: {Error}", request.Username, error);
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResult(error ?? "Login failed"));
            }

            var user = await _userRepo.GetByUsernameAsync(request.Username);
            if (user == null)
            {
                return Unauthorized(ApiResponse<LoginResponse>.ErrorResult("User not found"));
            }

            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "1440");

            var response = new LoginResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    WorkerType = user.WorkerType?.ToString(),
                    Pin = user.Pin,
                    EmployeeId = user.Pin ?? user.Username,  // PIN is employeeId for workers
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt
                }
            };

            _logger.LogInformation("User {Username} logged in successfully", request.Username);

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Login successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login for username: {Username}", request.Username);
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("An error occurred during login"));
        }
    }

    [HttpPost("login-gps")]
    [ProducesResponseType(typeof(ApiResponse<LoginWithGPSResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginWithGPSResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LoginWithGPSResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> LoginWithGPS([FromBody] LoginWithGPSRequest request)
    {
        try
        {
            _logger.LogInformation("GPS login attempt with PIN");

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return BadRequest(ApiResponse<LoginWithGPSResponse>.ErrorResult(errors));
            }

            // Get worker by PIN
            var user = await _userRepo.GetByPinAsync(request.Pin);
            if (user == null)
            {
                _logger.LogWarning("GPS login failed - Invalid PIN");
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResult("الرقم السري غير صحيح"));
            }

            // Check if user account is active
            if (user.Status != Core.Enums.UserStatus.Active)
            {
                _logger.LogWarning("GPS login failed - User {UserId} is not active", user.UserId);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResult("حساب المستخدم غير نشط"));
            }

            // Validate GPS location against assigned zones
            var userWithZones = await _userRepo.GetUserWithZonesAsync(user.UserId);
            var assignedZoneIds = userWithZones?.AssignedZones?.Where(uz => uz.IsActive).Select(uz => uz.ZoneId).ToList() ?? new List<int>();

            Core.Entities.Zone? validZone = null;
            bool isInValidZone = false;

            // Developer mode - skip geofencing if disabled
            var disableGeofencing = _configuration.GetValue<bool>("DeveloperMode:DisableGeofencing", false);
            if (disableGeofencing)
            {
                _logger.LogWarning("Developer mode: Geofencing disabled - allowing login from any location");
                // Get first assigned zone
                if (assignedZoneIds.Any())
                {
                    // Get assigned zones for this user
                    var assignedZones = await _zoneRepo.GetZonesByIdsAsync(assignedZoneIds);
                    validZone = assignedZones.FirstOrDefault();
                }
                isInValidZone = true;  // Skip validation in dev mode
            }
            else if (assignedZoneIds.Any())
            {
                // Get assigned zones for this user
                var assignedZones = (await _zoneRepo.GetZonesByIdsAsync(assignedZoneIds))
                    .Where(z => z.IsActive)
                    .ToList();

                // Create point from GPS coordinates
                var point = NetTopologySuite.Geometries.GeometryFactory.Default
                    .CreatePoint(new NetTopologySuite.Geometries.Coordinate(request.Longitude, request.Latitude));

                // 30 meter buffer (approximately 0.0003 degrees)
                const double BufferToleranceDegrees = 0.0003;

                foreach (var zone in assignedZones)
                {
                    if (zone.Boundary != null)
                    {
                        // Check if worker is inside zone
                        if (zone.Boundary.Contains(point))
                        {
                            validZone = zone;
                            isInValidZone = true;
                            break;
                        }

                        // Check if within buffer zone
                        if (zone.Boundary.Distance(point) <= BufferToleranceDegrees)
                        {
                            validZone = zone;
                            isInValidZone = true;
                            _logger.LogInformation("Worker {UserId} validated via Buffer Zone ({Distance} deg)", user.UserId, zone.Boundary.Distance(point));
                            break;
                        }
                    }
                }

                // Reject if not in any zone
                if (validZone == null)
                {
                    _logger.LogWarning("GPS login rejected - Worker {UserId} is outside all assigned zones", user.UserId);
                    return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResult("أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور."));
                }
            }
            else
            {
                // No zones assigned - worker must have at least one zone
                _logger.LogWarning("GPS login rejected - Worker {UserId} has NO assigned zones", user.UserId);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResult("لا يوجد مناطق عمل مخصصة لهذا المستخدم. يرجى مراجعة المشرف."));
            }

            // Generate JWT token
            var (success, token, refreshToken, error) = await _authService.GenerateTokenForUserAsync(user);

            if (!success)
            {
                _logger.LogWarning("GPS login failed - Token generation error for user {UserId}: {Error}",
                    user.UserId, error);
                return Unauthorized(ApiResponse<LoginWithGPSResponse>.ErrorResult(error ?? "فشل تسجيل الدخول"));
            }

            // Create attendance record for check-in
            var attendance = new Core.Entities.Attendance
            {
                UserId = user.UserId,
                CheckInEventTime = DateTime.UtcNow,
                CheckInLatitude = request.Latitude,
                CheckInLongitude = request.Longitude,
                ZoneId = validZone?.ZoneId,
                IsValidated = isInValidZone,
                ValidationMessage = isInValidZone
                    ? "تم تسجيل الحضور بنجاح داخل المنطقة المخصصة"
                    : "تم تسجيل الحضور (خارج المناطق المخصصة)",
                Status = Core.Enums.AttendanceStatus.CheckedIn
            };

            await _attendanceRepo.AddAsync(attendance);
            await _context.SaveChangesAsync();

            _logger.LogInformation("User {UserId} ({FullName}) logged in with GPS and attendance created. Attendance ID: {AttendanceId}",
                user.UserId, user.FullName, attendance.AttendanceId);

            // Build response
            var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "1440");

            var response = new LoginWithGPSResponse
            {
                Success = true,
                Token = token,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(expirationMinutes),
                User = new UserDto
                {
                    UserId = user.UserId,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Role = user.Role.ToString(),
                    WorkerType = user.WorkerType?.ToString(),
                    Pin = user.Pin,
                    EmployeeId = user.Pin ?? user.Username,  // PIN is employeeId for workers
                    PhoneNumber = user.PhoneNumber,
                    CreatedAt = user.CreatedAt
                },
                Attendance = new AttendanceResponse
                {
                    AttendanceId = attendance.AttendanceId,
                    UserId = attendance.UserId,
                    UserName = user.FullName,
                    ZoneId = attendance.ZoneId,
                    ZoneName = validZone?.ZoneName,
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

            return Ok(ApiResponse<LoginWithGPSResponse>.SuccessResult(response, "GPS login and check-in successful"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during GPS login");
            return StatusCode(500, ApiResponse<LoginWithGPSResponse>.ErrorResult("حدث خطأ أثناء تسجيل الدخول"));
        }
    }

    [HttpPost("register")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        try
        {
            _logger.LogInformation("Registration attempt for username: {Username}", request.Username);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)
                    .ToList();
                return BadRequest(ApiResponse<UserDto>.ErrorResult(errors));
            }

            // Handle PIN for workers
            string? workerPin = request.Pin;
            if (request.Role == Core.Enums.UserRole.Worker)
            {
                // Auto-generate PIN if not provided
                if (string.IsNullOrEmpty(workerPin))
                {
                    workerPin = await GenerateUniquePinAsync();
                }
                else
                {
                    // Check PIN uniqueness if provided
                    var isPinUnique = await _userRepo.IsPinUniqueAsync(workerPin);
                    if (!isPinUnique)
                    {
                        return BadRequest(ApiResponse<UserDto>.ErrorResult("PIN already exists. Please use a different PIN."));
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

            var (success, createdUser, error) = await _authService.RegisterAsync(user, request.Password);

            if (!success)
            {
                _logger.LogWarning("Registration failed for username: {Username}. Error: {Error}", request.Username, error);
                return BadRequest(ApiResponse<UserDto>.ErrorResult(error ?? "Registration failed"));
            }

            var userDto = new UserDto
            {
                UserId = createdUser!.UserId,
                Username = createdUser.Username,
                FullName = createdUser.FullName,
                Email = createdUser.Email,
                Role = createdUser.Role.ToString(),
                WorkerType = createdUser.WorkerType?.ToString(),
                Pin = createdUser.Pin, // Show PIN to admin
                PhoneNumber = createdUser.PhoneNumber,
                CreatedAt = createdUser.CreatedAt
            };

            _logger.LogInformation("User {Username} registered successfully", request.Username);

            return CreatedAtAction(nameof(GetProfile), null, ApiResponse<UserDto>.SuccessResult(userDto, "User registered successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during registration for username: {Username}", request.Username);
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred during registration"));
        }
    }

    [HttpGet("profile")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetProfile()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(ApiResponse<UserDto>.ErrorResult("Invalid token"));
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<UserDto>.ErrorResult("User not found"));
            }

            var userDto = new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                WorkerType = user.WorkerType?.ToString(),
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };

            return Ok(ApiResponse<UserDto>.SuccessResult(userDto));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving user profile");
            return StatusCode(500, ApiResponse<UserDto>.ErrorResult("An error occurred while retrieving profile"));
        }
    }

    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Refresh([FromBody] string refreshToken)
    {
        try
        {
            _logger.LogInformation("Token refresh attempt");

            var (success, newToken, error) = await _authService.RefreshTokenAsync(refreshToken);

            if (!success)
            {
                return BadRequest(ApiResponse<LoginResponse>.ErrorResult(error ?? "Token refresh failed"));
            }

            var response = new LoginResponse
            {
                Success = true,
                Token = newToken,
                ExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "1440"))
            };

            return Ok(ApiResponse<LoginResponse>.SuccessResult(response, "Token refreshed successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during token refresh");
            return StatusCode(500, ApiResponse<LoginResponse>.ErrorResult("An error occurred during token refresh"));
        }
    }

    [HttpPost("logout")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Logout()
    {
        try
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(userIdClaim, out var userId))
            {
                await _authService.LogoutAsync(userId);
                _logger.LogInformation("User {UserId} logged out", userId);
            }

            return Ok(ApiResponse<string>.SuccessResult("Logged out successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during logout");
            return StatusCode(500, ApiResponse<string>.ErrorResult("An error occurred during logout"));
        }
    }

    [HttpPost("register-fcm-token")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RegisterFcmToken([FromBody] RegisterFcmTokenRequest request)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ApiResponse<object>.ErrorResult("Invalid request"));
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return Unauthorized(ApiResponse<object>.ErrorResult("Invalid user token"));
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null)
            {
                return NotFound(ApiResponse<object>.ErrorResult("User not found"));
            }

            user.FcmToken = request.FcmToken;
            await _userRepo.UpdateAsync(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation("FCM token registered for user {UserId}", userId);

            return Ok(ApiResponse<object?>.SuccessResponse(null, "FCM token registered successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error registering FCM token");
            return StatusCode(500, ApiResponse<object>.ErrorResult("An error occurred while registering FCM token"));
        }
    }

    // Use Random.Shared (thread-safe) to avoid duplicate PINs
    private static readonly Random _randomGenerator = Random.Shared;

    private async Task<string> GenerateUniquePinAsync()
    {
        string pin;
        bool isUnique;

        do
        {
            pin = _randomGenerator.Next(1000, 9999).ToString();
            isUnique = await _userRepo.IsPinUniqueAsync(pin);
        } while (!isUnique);

        return pin;
    }
}
