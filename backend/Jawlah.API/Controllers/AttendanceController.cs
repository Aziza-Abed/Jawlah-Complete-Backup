using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceRepository _attendanceRepo;
    private readonly IUserRepository _userRepo;
    private readonly IZoneRepository _zoneRepo;
    private readonly JawlahDbContext _context;
    private readonly IGisService _gisService;
    private readonly ILogger<AttendanceController> _logger;

    public AttendanceController(
        IAttendanceRepository attendanceRepo,
        IUserRepository userRepo,
        IZoneRepository zoneRepo,
        JawlahDbContext context,
        IGisService gisService,
        ILogger<AttendanceController> logger)
    {
        _attendanceRepo = attendanceRepo;
        _userRepo = userRepo;
        _zoneRepo = zoneRepo;
        _context = context;
        _gisService = gisService;
        _logger = logger;
    }

    [HttpPost("checkin")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResult("Invalid token"));

            _logger.LogInformation("Check-in attempt for user {UserId} at ({Lat}, {Lon})",
                userId, request.Latitude, request.Longitude);

            // Use transaction to prevent concurrent check-ins
            await using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var hasActive = await _attendanceRepo.HasActiveAttendanceAsync(userId.Value);
                if (hasActive)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(ApiResponse<AttendanceResponse>.ErrorResult("You already have an active check-in"));
                }

                var zone = await _gisService.ValidateLocationAsync(request.Latitude, request.Longitude);

                // Manual check-in requires being inside the zone
                if (zone == null)
                {
                    await transaction.RollbackAsync();
                    return BadRequest(ApiResponse<AttendanceResponse>.ErrorResult("أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور."));
                }

                var attendance = new Attendance
                {
                    UserId = userId.Value,
                    ZoneId = zone.ZoneId,
                    CheckInEventTime = DateTime.UtcNow,
                    CheckInSyncTime = DateTime.UtcNow,
                    CheckInLatitude = request.Latitude,
                    CheckInLongitude = request.Longitude,
                    IsValidated = true,
                    ValidationMessage = "تم تسجيل الحضور بنجاح داخل المنطقة المخصصة",
                    Status = AttendanceStatus.CheckedIn,
                    IsSynced = true,
                    SyncVersion = 1
                };

                await _attendanceRepo.AddAsync(attendance);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var user = await _userRepo.GetByIdAsync(userId.Value);

                var response = new AttendanceResponse
            {
                AttendanceId = attendance.AttendanceId,
                UserId = attendance.UserId,
                UserName = user?.FullName ?? "Unknown",
                ZoneId = attendance.ZoneId,
                ZoneName = zone?.ZoneName,
                CheckInEventTime = attendance.CheckInEventTime,
                CheckInLatitude = attendance.CheckInLatitude,
                CheckInLongitude = attendance.CheckInLongitude,
                IsValidated = attendance.IsValidated,
                ValidationMessage = attendance.ValidationMessage,
                Status = attendance.Status,
                CreatedAt = attendance.CheckInEventTime
            };

                _logger.LogInformation("User {UserId} checked in successfully at zone {ZoneId}", userId, zone?.ZoneId);

                return Ok(ApiResponse<AttendanceResponse>.SuccessResult(response, "Checked in successfully"));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in");
            return StatusCode(500, ApiResponse<AttendanceResponse>.ErrorResult("An error occurred during check-in"));
        }
    }

    [HttpPost("checkout")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResult("Invalid token"));

            _logger.LogInformation("Check-out attempt for user {UserId}", userId);

            var attendance = await _attendanceRepo.GetTodayAttendanceAsync(userId.Value);
            if (attendance == null)
            {
                return BadRequest(ApiResponse<AttendanceResponse>.ErrorResult("No active check-in found"));
            }

            if (attendance.Status != AttendanceStatus.CheckedIn)
            {
                return BadRequest(ApiResponse<AttendanceResponse>.ErrorResult("You are not currently checked in"));
            }

            attendance.CheckOutEventTime = DateTime.UtcNow;
            attendance.CheckOutSyncTime = DateTime.UtcNow;
            attendance.CheckOutLatitude = request.Latitude;
            attendance.CheckOutLongitude = request.Longitude;
            attendance.Status = AttendanceStatus.CheckedOut;
            attendance.WorkDuration = attendance.CheckOutEventTime - attendance.CheckInEventTime;

            await _attendanceRepo.UpdateAsync(attendance);
            await _context.SaveChangesAsync();

            var user = await _userRepo.GetByIdAsync(userId.Value);
            var zone = attendance.ZoneId.HasValue
                ? await _zoneRepo.GetByIdAsync(attendance.ZoneId.Value)
                : null;

            var response = new AttendanceResponse
            {
                AttendanceId = attendance.AttendanceId,
                UserId = attendance.UserId,
                UserName = user?.FullName ?? "Unknown",
                ZoneId = attendance.ZoneId,
                ZoneName = zone?.ZoneName,
                CheckInEventTime = attendance.CheckInEventTime,
                CheckOutEventTime = attendance.CheckOutEventTime,
                CheckInLatitude = attendance.CheckInLatitude,
                CheckInLongitude = attendance.CheckInLongitude,
                CheckOutLatitude = attendance.CheckOutLatitude,
                CheckOutLongitude = attendance.CheckOutLongitude,
                WorkDuration = attendance.WorkDuration,
                IsValidated = attendance.IsValidated,
                ValidationMessage = attendance.ValidationMessage,
                Status = attendance.Status,
                CreatedAt = attendance.CheckInEventTime
            };

            _logger.LogInformation("User {UserId} checked out successfully. Work duration: {Duration}",
                userId, attendance.WorkDuration);

            return Ok(ApiResponse<AttendanceResponse>.SuccessResult(response, "Checked out successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-out");
            return StatusCode(500, ApiResponse<AttendanceResponse>.ErrorResult("An error occurred during check-out"));
        }
    }

    [HttpGet("today")]
    [ProducesResponseType(typeof(ApiResponse<AttendanceResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTodayAttendance()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResult("Invalid token"));

            var attendance = await _attendanceRepo.GetTodayAttendanceAsync(userId.Value);
            if (attendance == null)
            {
                return Ok(ApiResponse<AttendanceResponse?>.SuccessResult(null, "No attendance record for today"));
            }

            var user = await _userRepo.GetByIdAsync(userId.Value);
            var zone = attendance.ZoneId.HasValue
                ? await _zoneRepo.GetByIdAsync(attendance.ZoneId.Value)
                : null;

            var response = new AttendanceResponse
            {
                AttendanceId = attendance.AttendanceId,
                UserId = attendance.UserId,
                UserName = user?.FullName ?? "Unknown",
                ZoneId = attendance.ZoneId,
                ZoneName = zone?.ZoneName,
                CheckInEventTime = attendance.CheckInEventTime,
                CheckOutEventTime = attendance.CheckOutEventTime,
                CheckInLatitude = attendance.CheckInLatitude,
                CheckInLongitude = attendance.CheckInLongitude,
                CheckOutLatitude = attendance.CheckOutLatitude,
                CheckOutLongitude = attendance.CheckOutLongitude,
                WorkDuration = attendance.WorkDuration,
                IsValidated = attendance.IsValidated,
                ValidationMessage = attendance.ValidationMessage,
                Status = attendance.Status,
                CreatedAt = attendance.CheckInEventTime
            };

            return Ok(ApiResponse<AttendanceResponse>.SuccessResult(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's attendance");
            return StatusCode(500, ApiResponse<AttendanceResponse>.ErrorResult("An error occurred"));
        }
    }

    [HttpGet("history")]
    [ProducesResponseType(typeof(ApiResponse<List<AttendanceResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAttendanceHistory(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<List<AttendanceResponse>>.ErrorResult("Invalid token"));

            var from = fromDate ?? DateTime.Today.AddMonths(-1);
            var to = toDate ?? DateTime.Today.AddDays(1);

            var attendances = await _attendanceRepo.GetUserAttendanceHistoryAsync(userId.Value, from, to);

            var responses = attendances.Select(a => new AttendanceResponse
            {
                AttendanceId = a.AttendanceId,
                UserId = a.UserId,
                UserName = a.User?.FullName ?? "Unknown",
                ZoneId = a.ZoneId,
                ZoneName = a.Zone?.ZoneName,
                CheckInEventTime = a.CheckInEventTime,
                CheckOutEventTime = a.CheckOutEventTime,
                CheckInLatitude = a.CheckInLatitude,
                CheckInLongitude = a.CheckInLongitude,
                CheckOutLatitude = a.CheckOutLatitude,
                CheckOutLongitude = a.CheckOutLongitude,
                WorkDuration = a.WorkDuration,
                IsValidated = a.IsValidated,
                ValidationMessage = a.ValidationMessage,
                Status = a.Status,
                CreatedAt = a.CheckInEventTime
            }).ToList();

            return Ok(ApiResponse<List<AttendanceResponse>>.SuccessResult(responses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance history");
            return StatusCode(500, ApiResponse<List<AttendanceResponse>>.ErrorResult("An error occurred"));
        }
    }

}
