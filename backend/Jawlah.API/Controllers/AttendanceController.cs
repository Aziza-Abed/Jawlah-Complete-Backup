using AutoMapper;
using Jawlah.Core.DTOs.Attendance;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.API.Controllers;

// this controller handles checkin and checkout for workers
[Route("api/[controller]")]
public class AttendanceController : BaseApiController
{
    private readonly IAttendanceRepository _attendance;
    private readonly IUserRepository _users;
    private readonly IGisService _gis;
    private readonly ILogger<AttendanceController> _logger;
    private readonly IMapper _mapper;

    public AttendanceController(
        IAttendanceRepository attendance,
        IUserRepository users,
        IGisService gis,
        ILogger<AttendanceController> logger,
        IMapper mapper)
    {
        _attendance = attendance;
        _users = users;
        _gis = gis;
        _logger = logger;
        _mapper = mapper;
    }

    // worker checkin at the start of day
    [HttpPost("checkin")]
    public async Task<IActionResult> CheckIn([FromBody] CheckInRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

        // check if worker already checked in today
        var hasActive = await _attendance.HasActiveAttendanceAsync(userId.Value);
        if (hasActive)
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("لديك تسجيل حضور نشط بالفعل"));

        // validate location using gis service
        var zone = await _gis.ValidateLocationAsync(request.Latitude, request.Longitude, userId.Value);
        if (zone == null)
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("أنت خارج منطقة العمل المخصصة لك، لا يمكن تسجيل الحضور"));

        // create new attendance record
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

        try
        {
            await _attendance.AddAsync(attendance);
            await _attendance.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // this happen when two request come at same time
            _logger.LogWarning("Duplicate check-in attempt for user {UserId}", userId);
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("لديك تسجيل حضور نشط بالفعل"));
        }

        var response = _mapper.Map<AttendanceResponse>(attendance);
        return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response, "تم تسجيل الحضور بنجاح"));
    }

    // worker checkout at end of day
    [HttpPost("checkout")]
    public async Task<IActionResult> CheckOut([FromBody] CheckOutRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

        // check gps is not zero
        if (request.Latitude == 0 && request.Longitude == 0)
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("إحداثيات GPS غير صالحة (0, 0). يرجى التأكد من تفعيل GPS"));

        // check coords are inside work area
        if (request.Latitude < Core.Constants.GeofencingConstants.MinLatitude ||
            request.Latitude > Core.Constants.GeofencingConstants.MaxLatitude ||
            request.Longitude < Core.Constants.GeofencingConstants.MinLongitude ||
            request.Longitude > Core.Constants.GeofencingConstants.MaxLongitude)
        {
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("أنت خارج منطقة العمل المسموح بها"));
        }

        // get today attendance
        var attendance = await _attendance.GetTodayAttendanceAsync(userId.Value);
        if (attendance == null)
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("لا يوجد تسجيل حضور نشط"));

        // make sure user is checked in
        if (attendance.Status != AttendanceStatus.CheckedIn)
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("أنت لست مسجلاً للحضور حالياً"));

        // update attendance with checkout data
        var checkOutTime = DateTime.UtcNow;
        attendance.CheckOutEventTime = checkOutTime;
        attendance.CheckOutSyncTime = checkOutTime;
        attendance.CheckOutLatitude = request.Latitude;
        attendance.CheckOutLongitude = request.Longitude;
        attendance.Status = AttendanceStatus.CheckedOut;

        // calculate work duration - ensure it's not negative (can happen due to timezone issues)
        var duration = checkOutTime - attendance.CheckInEventTime;
        if (duration < TimeSpan.Zero)
        {
            _logger.LogWarning(
                "Negative work duration detected for user {UserId}. CheckIn: {CheckIn}, CheckOut: {CheckOut}. Using absolute value.",
                attendance.UserId, attendance.CheckInEventTime, checkOutTime);
            duration = duration.Duration(); // Get absolute value
        }
        // SQL Server Time type max is 23:59:59, cap at 23 hours for safety
        if (duration > TimeSpan.FromHours(23))
        {
            duration = TimeSpan.FromHours(23);
        }
        attendance.WorkDuration = duration;

        await _attendance.UpdateAsync(attendance);
        await _attendance.SaveChangesAsync();

        var response = _mapper.Map<AttendanceResponse>(attendance);
        return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response, "تم تسجيل الانصراف بنجاح"));
    }

    // get today attendance for current user
    [HttpGet("today")]
    public async Task<IActionResult> GetTodayAttendance()
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

        var attendance = await _attendance.GetTodayAttendanceAsync(userId.Value);
        if (attendance == null)
            return Ok(ApiResponse<AttendanceResponse?>.SuccessResponse(null, "لا يوجد سجل حضور لهذا اليوم"));

        var response = _mapper.Map<AttendanceResponse>(attendance);
        return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response));
    }

    // get attendance history for worker
    [HttpGet("history")]
    public async Task<IActionResult> GetAttendanceHistory(
        [FromQuery] DateTime? fromDate = null,
        [FromQuery] DateTime? toDate = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<List<AttendanceResponse>>.ErrorResponse("رمز غير صالح"));

        // default to last month if no date given
        var from = fromDate ?? DateTime.Today.AddMonths(-1);
        var to = toDate ?? DateTime.Today.AddDays(1);

        // fix bad pagination values
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var attendances = await _attendance.GetUserAttendanceHistoryAsync(userId.Value, from, to);

        // paginate the results
        var totalCount = attendances.Count();
        var pagedAttendances = attendances
            .OrderByDescending(a => a.CheckInEventTime)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        var responses = pagedAttendances.Select(a => _mapper.Map<AttendanceResponse>(a)).ToList();
        return Ok(ApiResponse<List<AttendanceResponse>>.SuccessResponse(responses));
    }

    // UR8: Request manual attendance (worker submits when GPS not available)
    [HttpPost("manual")]
    public async Task<IActionResult> RequestManualAttendance([FromBody] ManualAttendanceRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<AttendanceResponse>.ErrorResponse("رمز غير صالح"));

        // check if worker already has attendance today
        var hasActive = await _attendance.HasActiveAttendanceAsync(userId.Value);
        if (hasActive)
            return BadRequest(ApiResponse<AttendanceResponse>.ErrorResponse("لديك تسجيل حضور نشط بالفعل"));

        // create pending manual attendance
        var attendance = new Attendance
        {
            UserId = userId.Value,
            ZoneId = request.ZoneId,
            CheckInEventTime = request.CheckInTime ?? DateTime.UtcNow,
            CheckInLatitude = 0,
            CheckInLongitude = 0,
            IsValidated = false,
            ValidationMessage = "في انتظار موافقة المشرف",
            Status = AttendanceStatus.CheckedIn,
            IsManual = true,
            ManualReason = request.Reason,
            IsSynced = true,
            SyncVersion = 1
        };

        await _attendance.AddAsync(attendance);
        await _attendance.SaveChangesAsync();

        _logger.LogInformation("Manual attendance requested by user {UserId}, reason: {Reason}", userId, request.Reason);

        var response = _mapper.Map<AttendanceResponse>(attendance);
        return Ok(ApiResponse<AttendanceResponse>.SuccessResponse(response, "تم إرسال طلب الحضور اليدوي"));
    }

    // UR8: Get pending manual attendance requests (supervisor)
    [HttpGet("pending-manual")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetPendingManualAttendance()
    {
        // query database directly instead of loading all records
        var pending = await _attendance.GetPendingManualAttendanceAsync();

        var response = pending.Select(a => new
        {
            a.AttendanceId,
            a.UserId,
            WorkerName = a.User?.FullName,
            a.ZoneId,
            ZoneName = a.Zone?.ZoneName,
            a.CheckInEventTime,
            a.ManualReason,
            a.Status
        });

        return Ok(ApiResponse<object>.SuccessResponse(response));
    }

    // UR8: Approve or reject manual attendance (supervisor)
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ApproveManualAttendance(int id, [FromBody] ApproveAttendanceRequest request)
    {
        var supervisorId = GetCurrentUserId();
        if (supervisorId == null)
            return Unauthorized();

        var attendance = await _attendance.GetByIdAsync(id);
        if (attendance == null)
            return NotFound(ApiResponse<object>.ErrorResponse("السجل غير موجود"));

        if (!attendance.IsManual)
            return BadRequest(ApiResponse<object>.ErrorResponse("هذا السجل ليس حضوراً يدوياً"));

        attendance.IsValidated = request.Approved;
        attendance.ApprovedByUserId = supervisorId.Value;
        attendance.ApprovedAt = DateTime.UtcNow;
        attendance.ValidationMessage = request.Approved
            ? "تمت الموافقة من قبل المشرف"
            : $"تم الرفض: {request.RejectionReason}";

        if (!request.Approved)
        {
            attendance.Status = AttendanceStatus.CheckedOut;
            attendance.WorkDuration = TimeSpan.Zero;
        }

        await _attendance.UpdateAsync(attendance);
        await _attendance.SaveChangesAsync();

        _logger.LogInformation("Manual attendance {Id} {Action} by supervisor {SupervisorId}",
            id, request.Approved ? "approved" : "rejected", supervisorId);

        return Ok(ApiResponse<object?>.SuccessResponse(null,
            request.Approved ? "تمت الموافقة على الحضور" : "تم رفض الحضور"));
    }
}
