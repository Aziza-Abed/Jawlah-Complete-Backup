using AutoMapper;
using FollowUp.Core.DTOs.Appeals;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

// handles location validation appeals
[Route("api/[controller]")]
public class AppealsController : BaseApiController
{
    private readonly IAppealRepository _appeals;
    private readonly ITaskRepository _tasks;
    private readonly IUserRepository _users;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IMapper _mapper;
    private readonly ILogger<AppealsController> _logger;

    public AppealsController(
        IAppealRepository appeals,
        ITaskRepository tasks,
        IUserRepository users,
        IFileStorageService files,
        INotificationService notifications,
        IMapper mapper,
        ILogger<AppealsController> logger)
    {
        _appeals = appeals;
        _tasks = tasks;
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
            // Future: handle attendance check-in failures
            return BadRequest(ApiResponse<object>.ErrorResponse("طعون الحضور غير مدعومة حالياً"));
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
                await _notifications.SendAppealSubmittedToSupervisorsAsync(
                    request.EntityId,
                    task?.Title ?? "مهمة",
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
    public async Task<IActionResult> GetMyAppeals()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var appeals = await _appeals.GetUserAppealsAsync(userId.Value);
        var responses = new List<AppealResponse>();
        var taskCache = new Dictionary<int, string?>(); // avoid duplicate task queries

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

            responses.Add(response);
        }

        return Ok(ApiResponse<List<AppealResponse>>.SuccessResponse(responses));
    }

    // get appeal by ID
    [HttpGet("{id}")]
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

        // Supervisors can only see appeals from their own workers
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

        return Ok(ApiResponse<AppealResponse>.SuccessResponse(response));
    }

    // get all pending appeals (supervisors only see their workers' appeals)
    [HttpGet("pending")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetPendingAppeals()
    {
        var appeals = await _appeals.GetPendingAppealsAsync();
        var responses = new List<AppealResponse>();

        // SECURITY: Supervisors can only see appeals from their own workers
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();
        HashSet<int>? supervisorWorkerIds = null;

        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            var supervisorWorkers = await _users.GetWorkersBySupervisorAsync(currentUserId.Value);
            supervisorWorkerIds = supervisorWorkers.Select(w => w.UserId).ToHashSet();
        }

        var taskCache = new Dictionary<int, string?>(); // avoid duplicate task queries

        foreach (var appeal in appeals)
        {
            // Filter: Skip appeals from workers not under this supervisor
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

            responses.Add(response);
        }

        return Ok(ApiResponse<List<AppealResponse>>.SuccessResponse(responses));
    }

    // review an appeal (approve or reject) - supervisors only
    [HttpPost("{id}/review")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ReviewAppeal(int id, [FromBody] ReviewAppealRequest request)
    {
        var supervisorId = GetCurrentUserId();
        if (!supervisorId.HasValue)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var appeal = await _appeals.GetByIdAsync(id);
        if (appeal == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الطعن غير موجود"));

        // SECURITY: Supervisors can only review appeals from their own workers
        var currentRole = GetCurrentUserRole();
        if (currentRole == "Supervisor")
        {
            var worker = await _users.GetByIdAsync(appeal.UserId);
            if (worker?.SupervisorId != supervisorId.Value)
            {
                return Forbid();
            }
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

        await _appeals.UpdateAsync(appeal);
        await _appeals.SaveChangesAsync();

        _logger.LogInformation("Appeal {AppealId} {Action} by supervisor {SupervisorId}",
            id, request.Approved ? "approved" : "rejected", supervisorId.Value);

        return Ok(ApiResponse<object?>.SuccessResponse(null,
            request.Approved ? "تمت الموافقة على الطعن" : "تم رفض الطعن"));
    }
}
