using AutoMapper;
using FollowUp.Core.DTOs.Appeals;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

// handles location validation appeals
[Route("api/[controller]")]
[Tags("Appeals")]
public class AppealsController : BaseApiController
{
    private readonly IAppealRepository _appeals;
    private readonly ITaskRepository _tasks;
    private readonly IAttendanceRepository _attendance;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IMapper _mapper;
    private readonly ILogger<AppealsController> _logger;

    public AppealsController(
        IAppealRepository appeals,
        ITaskRepository tasks,
        IAttendanceRepository attendance,
        IUserRepository users,
        IFileStorageService files,
        INotificationService notifications,
        IMapper mapper,
        ILogger<AppealsController> logger)
    {
        _appeals = appeals;
        _tasks = tasks;
        _attendance = attendance;
        _users = users;
        _files = files;
        _notifications = notifications;
        _mapper = mapper;
        _logger = logger;
    }

    // submit an appeal against an auto-rejected task or failed attendance
    [HttpPost]
    [Authorize(Roles = "Worker")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(Summary = "submit an appeal with evidence")]
    public async Task<IActionResult> SubmitAppeal([FromForm] SubmitAppealRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("مستخدم غير موجود"));

        // Determine entity type and validate
        string entityType;
        double? expectedLat = null, expectedLng = null, workerLat = null, workerLng = null;
        int? distanceMeters = null;
        string? originalRejectionReason = null;
        Core.Entities.Task? task = null;

        if (request.AppealType == AppealType.TaskRejection)
        {
            entityType = "Task";
            task = await _tasks.GetByIdAsync(request.EntityId);
            if (task == null)
                return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

            // For team tasks check team membership; for individual tasks check direct assignment
            if (task.IsTeamTask)
            {
                if (!task.TeamId.HasValue || user.TeamId != task.TeamId)
                    return Forbid();
            }
            else if (task.AssignedToUserId != userId.Value)
            {
                return Forbid();
            }

            if (!task.IsAutoRejected)
                return BadRequest(ApiResponse<object>.ErrorResponse("هذه المهمة لم يتم رفضها تلقائياً"));

            // Check if appeal already exists
            if (await _appeals.HasAppealForEntityAsync(entityType, request.EntityId))
                return BadRequest(ApiResponse<object>.ErrorResponse("تم إرسال طعن لهذه المهمة بالفعل"));

            expectedLat = task.Latitude;
            expectedLng = task.Longitude;
            workerLat = task.RejectionLatitude;
            workerLng = task.RejectionLongitude;
            distanceMeters = task.RejectionDistanceMeters;
            originalRejectionReason = task.RejectionReason;
        }
        else // AttendanceFailure
        {
            entityType = "Attendance";
            var attendance = await _attendance.GetByIdAsync(request.EntityId);
            if (attendance == null)
                return NotFound(ApiResponse<object>.ErrorResponse("سجل الحضور غير موجود"));

            if (attendance.UserId != userId.Value)
                return Forbid();

            // Only allow appeals for rejected manual attendance
            if (attendance.ApprovalStatus != "Rejected")
                return BadRequest(ApiResponse<object>.ErrorResponse("سجل الحضور هذا لم يتم رفضه"));

            // Check if appeal already exists
            if (await _appeals.HasAppealForEntityAsync(entityType, request.EntityId))
                return BadRequest(ApiResponse<object>.ErrorResponse("تم إرسال طعن لسجل الحضور هذا بالفعل"));

            workerLat = attendance.CheckInLatitude;
            workerLng = attendance.CheckInLongitude;
            originalRejectionReason = attendance.ValidationMessage;
        }

        // Upload evidence photo if provided (optional)
        string? evidencePhotoUrl = null;
        if (request.EvidencePhoto != null)
        {
            if (!_files.ValidateImage(request.EvidencePhoto))
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة غير صالح"));

            evidencePhotoUrl = await _files.UploadImageAsync(request.EvidencePhoto, "appeals");
        }

        // Create appeal
        var appeal = new Appeal
        {
            MunicipalityId = user.MunicipalityId,
            AppealType = request.AppealType,
            EntityType = entityType,
            EntityId = request.EntityId,
            UserId = userId.Value,
            WorkerExplanation = Utils.InputSanitizer.SanitizeString(request.WorkerExplanation, 1000),
            WorkerLatitude = workerLat,
            WorkerLongitude = workerLng,
            ExpectedLatitude = expectedLat,
            ExpectedLongitude = expectedLng,
            DistanceMeters = distanceMeters,
            OriginalRejectionReason = originalRejectionReason,
            EvidencePhotoUrl = evidencePhotoUrl,
            Status = AppealStatus.Pending,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            IsSynced = true,
            SyncVersion = 1
        };

        try
        {
            await _appeals.AddAsync(appeal);
            await _appeals.SaveChangesAsync();

            _logger.LogInformation("Appeal {AppealId} submitted by user {UserId} for {EntityType} {EntityId}",
                appeal.AppealId, userId.Value, entityType, request.EntityId);

            // notify supervisors about the new appeal
            try
            {
                var entityTitle = request.AppealType == AppealType.TaskRejection
                    ? task?.Title ?? "مهمة"
                    : "طعن حضور";
                await _notifications.SendAppealSubmittedToSupervisorsAsync(
                    request.EntityId,
                    entityTitle,
                    user.FullName,
                    user.MunicipalityId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send appeal notification for appeal {AppealId}", appeal.AppealId);
            }

            return Ok(ApiResponse<object>.SuccessResponse(new
            {
                appealId = appeal.AppealId,
                message = "تم إرسال الطعن بنجاح"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to submit appeal for user {UserId}", userId.Value);

            // Cleanup uploaded photo if save failed
            if (evidencePhotoUrl != null)
            {
                await _files.DeleteImageAsync(evidencePhotoUrl);
            }

            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل في إرسال الطعن"));
        }
    }

    // get all appeals for the current user
    [HttpGet("my-appeals")]
    [Authorize(Roles = "Worker")]
    [SwaggerOperation(Summary = "get current user appeals")]
    public async Task<IActionResult> GetMyAppeals()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var appeals = await _appeals.GetUserAppealsAsync(userId.Value);
        var responses = new List<AppealResponse>();
        var taskCache = new Dictionary<int, string?>(); // avoid duplicate task queries
        var attendanceCache = new Dictionary<int, string?>(); // avoid duplicate attendance queries

        foreach (var appeal in appeals)
        {
            var response = _mapper.Map<AppealResponse>(appeal);

            if (appeal.EntityType == "Task")
            {
                if (!taskCache.TryGetValue(appeal.EntityId, out var title))
                {
                    var task = await _tasks.GetByIdAsync(appeal.EntityId);
                    title = task?.Title;
                    taskCache[appeal.EntityId] = title;
                }
                response.EntityTitle = title;
            }
            else if (appeal.EntityType == "Attendance")
            {
                if (!attendanceCache.TryGetValue(appeal.EntityId, out var title))
                {
                    var att = await _attendance.GetByIdAsync(appeal.EntityId);
                    title = att != null ? $"حضور {att.CheckInEventTime:yyyy-MM-dd}" : null;
                    attendanceCache[appeal.EntityId] = title;
                }
                response.EntityTitle = title;
            }

            responses.Add(response);
        }

        return Ok(ApiResponse<List<AppealResponse>>.SuccessResponse(responses));
    }

    // get appeal by ID
    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "get a single appeal by id")]
    public async Task<IActionResult> GetAppealById(int id)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var appeal = await _appeals.GetByIdAsync(id);
        if (appeal == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الطعن غير موجود"));

        // Workers can only see their own appeals
        if (userRole == "Worker" && appeal.UserId != userId.Value)
            return Forbid();

        // Supervisors can only see appeals from their own workers; Admin can see all
        if (userRole == "Supervisor")
        {
            var worker = await _users.GetByIdAsync(appeal.UserId);
            if (worker?.SupervisorId != userId.Value)
                return Forbid();
        }

        var response = _mapper.Map<AppealResponse>(appeal);

        // Set entity title
        if (appeal.EntityType == "Task")
        {
            var task = await _tasks.GetByIdAsync(appeal.EntityId);
            response.EntityTitle = task?.Title;
        }
        else if (appeal.EntityType == "Attendance")
        {
            var att = await _attendance.GetByIdAsync(appeal.EntityId);
            response.EntityTitle = att != null ? $"حضور {att.CheckInEventTime:yyyy-MM-dd}" : null;
        }

        return Ok(ApiResponse<AppealResponse>.SuccessResponse(response));
    }

    // get all pending appeals (supervisor sees their workers' appeals only)
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get all pending appeals")]
    public async Task<IActionResult> GetPendingAppeals()
    {
        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var appeals = await _appeals.GetPendingAppealsAsync();
        var responses = new List<AppealResponse>();

        var userRole = GetCurrentUserRole();

        // Supervisors only see appeals from their own workers; Admin sees all
        HashSet<int>? supervisorWorkerIds = null;
        if (userRole == "Supervisor")
        {
            var supervisorWorkers = await _users.GetWorkersBySupervisorAsync(currentUserId.Value);
            supervisorWorkerIds = supervisorWorkers.Select(w => w.UserId).ToHashSet();
        }

        var taskCache = new Dictionary<int, string?>(); // avoid duplicate task queries
        var attendanceCache = new Dictionary<int, string?>(); // avoid duplicate attendance queries

        foreach (var appeal in appeals)
        {
            // Skip appeals from workers not under this supervisor (Admin sees all)
            if (supervisorWorkerIds != null && !supervisorWorkerIds.Contains(appeal.UserId))
            {
                continue;
            }

            var response = _mapper.Map<AppealResponse>(appeal);

            if (appeal.EntityType == "Task")
            {
                if (!taskCache.TryGetValue(appeal.EntityId, out var title))
                {
                    var task = await _tasks.GetByIdAsync(appeal.EntityId);
                    title = task?.Title;
                    taskCache[appeal.EntityId] = title;
                }
                response.EntityTitle = title;
            }
            else if (appeal.EntityType == "Attendance")
            {
                if (!attendanceCache.TryGetValue(appeal.EntityId, out var title))
                {
                    var att = await _attendance.GetByIdAsync(appeal.EntityId);
                    title = att != null ? $"حضور {att.CheckInEventTime:yyyy-MM-dd}" : null;
                    attendanceCache[appeal.EntityId] = title;
                }
                response.EntityTitle = title;
            }

            responses.Add(response);
        }

        return Ok(ApiResponse<List<AppealResponse>>.SuccessResponse(responses));
    }

    // review an appeal (approve or reject) - supervisors and admins
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "approve or reject an appeal")]
    public async Task<IActionResult> ReviewAppeal(int id, [FromBody] ReviewAppealRequest request)
    {
        var supervisorId = GetCurrentUserId();
        if (!supervisorId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var appeal = await _appeals.GetByIdAsync(id);
        if (appeal == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الطعن غير موجود"));

        // Supervisors can only review appeals from their own workers; Admin can review all
        var userRole = GetCurrentUserRole();
        if (userRole == "Supervisor")
        {
            var worker = await _users.GetByIdAsync(appeal.UserId);
            if (worker?.SupervisorId != supervisorId.Value)
                return Forbid();
        }

        if (appeal.Status != AppealStatus.Pending)
            return BadRequest(ApiResponse<object>.ErrorResponse("هذا الطعن تمت مراجعته بالفعل"));

        // Update appeal status
        appeal.Status = request.Approved ? AppealStatus.Approved : AppealStatus.Rejected;
        appeal.ReviewedByUserId = supervisorId.Value;
        appeal.ReviewedAt = DateTime.UtcNow;
        appeal.ReviewNotes = Utils.InputSanitizer.SanitizeString(request.ReviewNotes, 1000);
        appeal.UpdatedAt = DateTime.UtcNow;
        appeal.SyncVersion++;

        // If approved, reinstate the task or attendance
        if (request.Approved && appeal.EntityType == "Task")
        {
            var task = await _tasks.GetByIdAsync(appeal.EntityId);
            if (task != null)
            {
                // Change task status back to Completed
                task.Status = Core.Enums.TaskStatus.Completed;
                task.IsAutoRejected = false;
                task.RejectionReason = null;
                task.RejectedAt = null;
                task.CompletedAt = DateTime.UtcNow;
                task.SyncVersion++;

                await _tasks.UpdateAsync(task);

                _logger.LogInformation("Task {TaskId} reinstated after appeal {AppealId} approval by supervisor {SupervisorId}",
                    task.TaskId, appeal.AppealId, supervisorId.Value);
            }
        }
        else if (request.Approved && appeal.EntityType == "Attendance")
        {
            var attendance = await _attendance.GetByIdAsync(appeal.EntityId);
            if (attendance != null)
            {
                // Reinstate the attendance record
                attendance.IsValidated = true;
                attendance.ApprovalStatus = "Approved";
                attendance.ApprovedByUserId = supervisorId.Value;
                attendance.ApprovedAt = DateTime.UtcNow;
                attendance.ValidationMessage = "تمت الموافقة بعد الطعن";
                attendance.SyncVersion++;

                await _attendance.UpdateAsync(attendance);

                _logger.LogInformation("Attendance {AttendanceId} reinstated after appeal {AppealId} approval by supervisor {SupervisorId}",
                    attendance.AttendanceId, appeal.AppealId, supervisorId.Value);
            }
        }

        await _appeals.UpdateAsync(appeal);
        await _appeals.SaveChangesAsync();

        _logger.LogInformation("Appeal {AppealId} {Action} by supervisor {SupervisorId}",
            id, request.Approved ? "approved" : "rejected", supervisorId.Value);

        // Notify the worker about the appeal outcome
        try
        {
            var statusText = request.Approved ? "تمت الموافقة على طعنك" : "تم رفض طعنك";
            var details = request.Approved
                ? "تمت الموافقة على طعنك وإعادة المهمة/الحضور إلى حالتها الصحيحة"
                : $"تم رفض طعنك. {(string.IsNullOrEmpty(appeal.ReviewNotes) ? "" : $"السبب: {appeal.ReviewNotes}")}";
            await _notifications.SendSystemAlertAsync(appeal.UserId, $"{statusText}: {details}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send appeal review notification to worker for appeal {AppealId}", appeal.AppealId);
        }

        return Ok(ApiResponse<object?>.SuccessResponse(null,
            request.Approved ? "تمت الموافقة على الطعن" : "تم رفض الطعن"));
    }
}
