using AutoMapper;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceRepository _attendance;
    private readonly IGisService _gis;
    private readonly ILogger<AttendanceController> _logger;
    private readonly IMapper _mapper;

    public AttendanceController(
        IAttendanceRepository attendance,
        IGisService gis,
        ILogger<AttendanceController> logger,
        IMapper mapper)
    {
        _attendance = attendance;
        _gis = gis;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        try
        {
            // check if the user is logged in
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

            // check if the user already has an active attendance session
            var hasActive = await _attendance.HasActiveAttendanceAsync(userId.Value);
            if (hasActive)
            {
                return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("لديك تسجيل حضور نشط بالفعل"));
            }

            // validate the user location using GIS service
            var zone = await _gis.ValidateLocationAsync(request.Latitude, request.Longitude, userId.Value);

            if (zone == null)
            {
                return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور"));
            }

            // if location is valid, create a new record
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

            await _attendance.AddAsync(attendance);
            await _attendance.SaveChangesAsync();

            // return the saved record
            var response = _mapper.Map<AttendanceResponse>(attendance);

            return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response, "تم تسجيل الحضور بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-in");
            return StatusCode(500, ApiResponse<AttendanceResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الحضور"));
        }
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

            var attendance = await _attendance.GetTodayAttendanceAsync(userId.Value);
            if (attendance == null)
            {
                return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("لا يوجد تسجيل حضور نشط"));
            }

            if (attendance.Status != AttendanceStatus.CheckedIn)
            {
                return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("أنت لست مسجلاً للحضور حالياً"));
            }

            attendance.CheckOutEventTime = DateTime.UtcNow;
            attendance.CheckOutSyncTime = DateTime.UtcNow;
            attendance.CheckOutLatitude = request.Latitude;
            attendance.CheckOutLongitude = request.Longitude;
            attendance.Status = AttendanceStatus.CheckedOut;

            attendance.WorkDuration = attendance.CheckOutEventTime - attendance.CheckInEventTime;

            await _attendance.UpdateAsync(attendance);
            await _attendance.SaveChangesAsync();

            var response = _mapper.Map<AttendanceResponse>(attendance);

            return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response, "تم تسجيل الانصراف بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during check-out");
            return StatusCode(500, ApiResponse<AttendanceResponse>.ErrorResponse("حدث خطأ أثناء تسجيل الانصراف"));
        }
    }

    [HttpGet("today")]
    public async Task<IActionResult> GetTodayAttendance()
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

            var attendance = await _attendance.GetTodayAttendanceAsync(userId.Value);
            if (attendance == null)
            {
                return Ok(ApiResponse<AttendanceResponse?>.SuccessResponse(null, "لا يوجد سجل حضور لهذا اليوم"));
            }

            var response = _mapper.Map<AttendanceResponse>(attendance);

            return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving today's attendance");
            return StatusCode(500, ApiResponse<AttendanceResponse>.ErrorResponse("حدث خطأ"));
        }
    }

    [HttpGet("history")]
    public async Task<IActionResult> GetAttendanceHistory(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        try
        {
            var userId = GetCurrentUserId();
            if (userId == null)
                return Unauthorized(ApiResponse<List<AttendanceResponse>>.ErrorResponse("رمز غير صالح"));

            var from = fromDate ?? DateTime.Today.AddMonths(-1);
            var to = toDate ?? DateTime.Today.AddDays(1);

            if (page < 1) page = 1;
            if (pageSize < 1 || pageSize > 100) pageSize = 50;

            var attendances = await _attendance.GetUserAttendanceHistoryAsync(userId.Value, from, to);

            var totalCount = attendances.Count();
            var pagedAttendances = attendances
                .OrderByDescending(a => a.CheckInEventTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var responses = pagedAttendances.Select(a => _mapper.Map<AttendanceResponse>(a)).ToList();

            return Ok(ApiResponse<List<AttendanceResponse>>.SuccessResponse(responses));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving attendance history");
            return StatusCode(500, ApiResponse<List<AttendanceResponse>>.ErrorResponse("حدث خطأ"));
        }
    }

}
