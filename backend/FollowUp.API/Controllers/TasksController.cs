using System.Security.Claims;
using AutoMapper;
using FollowUp.API.Models;
using FollowUp.API.Utils;
using FollowUp.Core.Constants;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Tasks;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskEntity = FollowUp.Core.Entities.Task;
using TaskStatus = FollowUp.Core.Enums.TaskStatus;
using UserRole = FollowUp.Core.Enums.UserRole;

namespace FollowUp.API.Controllers;

// this controller handle all task operations
[Route("api/[controller]")]
public class TasksController : BaseApiController
{
    private readonly ITaskRepository _tasks;
    private readonly IPhotoRepository _photos;
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly ILogger<TasksController> _logger;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IMapper _mapper;
    private readonly IConfiguration _config;

    // max number of active tasks per worker (fair distribution)
    private const int MaxActiveTasksPerWorker = 5;

    // Auto-rejection thresholds for location verification (see AppConstants)
    private static readonly int HardRejectDistanceMeters = AppConstants.HardRejectDistanceMeters;
    private static readonly int WarningDistanceMeters = AppConstants.WarningDistanceMeters;

    public TasksController(
        ITaskRepository tasks,
        IPhotoRepository photos,
        IUserRepository users,
        IZoneRepository zones,
        ILogger<TasksController> logger,
        IFileStorageService files,
        INotificationService notifications,
        IMapper mapper,
        IConfiguration config)
    {
        _tasks = tasks;
        _photos = photos;
        _users = users;
        _zones = zones;
        _logger = logger;
        _files = files;
        _notifications = notifications;
        _mapper = mapper;
        _config = config;
    }

    // get tasks for current worker
    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // fix bad pagination values
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        // parse status filter
        TaskStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<TaskStatus>(status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }
        }

        // parse priority filter
        FollowUp.Core.Enums.TaskPriority? priorityEnum = null;
        if (!string.IsNullOrEmpty(priority))
        {
            if (Enum.TryParse<FollowUp.Core.Enums.TaskPriority>(priority, true, out var parsedPriority))
            {
                priorityEnum = parsedPriority;
            }
        }

        var tasks = await _tasks.GetUserTasksAsync(userId.Value, statusEnum, priorityEnum, page, pageSize);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            tasks.Select(t => _mapper.Map<TaskResponse>(t))));
    }

    // get single task by id
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetTaskById(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userRole = GetCurrentUserRole();

        // workers can only see their own tasks
        if (userRole == "Worker" && task.AssignedToUserId != userId.Value)
            return Forbid();

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    // create new task by supervisor or admin
    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // get current user to get their municipality
        var currentUser = await _users.GetByIdAsync(userId.Value);
        if (currentUser == null)
            return Unauthorized();

        // clean inputs to prevent xss
        var sanitizedTitle = InputSanitizer.SanitizeString(request.Title, 200);
        var sanitizedDescription = InputSanitizer.SanitizeString(request.Description, 2000);
        var sanitizedLocation = InputSanitizer.SanitizeString(request.LocationDescription, 500);

        // check fair distribution - worker shouldn't have too many active tasks
        var workerActiveTasks = await GetActiveTaskCount(request.AssignedToUserId);
        if (workerActiveTasks >= MaxActiveTasksPerWorker)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                $"العامل لديه بالفعل {workerActiveTasks} مهام نشطة. الحد الأقصى هو {MaxActiveTasksPerWorker} مهام."));
        }

        // check for task time conflicts on the same day
        if (request.DueDate.HasValue)
        {
            var hasConflict = await HasTaskConflict(request.AssignedToUserId, request.DueDate.Value, null);
            if (hasConflict)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "العامل لديه مهمة أخرى في نفس اليوم. يرجى اختيار تاريخ مختلف."));
            }
        }

        // create task entity
        var task = new TaskEntity
        {
            Title = sanitizedTitle,
            Description = sanitizedDescription,
            MunicipalityId = currentUser.MunicipalityId,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = userId.Value,
            ZoneId = request.ZoneId,
            Priority = request.Priority,
            Status = TaskStatus.Pending,
            TaskType = request.TaskType,
            RequiresPhotoProof = request.RequiresPhotoProof,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            DueDate = request.DueDate,
            Latitude = request.Latitude,         // NEW: Map GPS coordinates
            Longitude = request.Longitude,       // NEW: Map GPS coordinates
            LocationDescription = sanitizedLocation,
            CreatedAt = DateTime.UtcNow,
            EventTime = DateTime.UtcNow,
            SyncTime = DateTime.UtcNow,
            IsSynced = true,
            SyncVersion = 1
        };

        await _tasks.AddAsync(task);
        await _tasks.SaveChangesAsync();

        // send notification to assigned worker
        try
        {
            await _notifications.SendTaskAssignedNotificationAsync(
                task.AssignedToUserId,
                task.TaskId,
                task.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send task assignment notification for task {TaskId}", task.TaskId);
        }

        return CreatedAtAction(nameof(GetTaskById), new { id = task.TaskId },
            ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    // get all tasks for supervisors (filtered to their workers only)
    [HttpGet("all")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAllTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        [FromQuery] string? status = null,
        [FromQuery] string? priority = null,
        [FromQuery] int? workerId = null,
        [FromQuery] int? zoneId = null)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var allTasks = await _tasks.GetAllAsync();

        // SECURITY FIX: Supervisors can only see tasks assigned to their workers
        var currentRole = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var currentUserId = GetCurrentUserId();
        if (currentRole?.Equals("Supervisor", StringComparison.OrdinalIgnoreCase) == true && currentUserId.HasValue)
        {
            // Get workers assigned to this supervisor
            var myWorkers = await _users.GetByRoleAsync(UserRole.Worker);
            var myWorkerIds = myWorkers
                .Where(w => w.SupervisorId == currentUserId.Value)
                .Select(w => w.UserId)
                .ToHashSet();

            allTasks = allTasks.Where(t => myWorkerIds.Contains(t.AssignedToUserId));
        }

        // filter by worker
        if (workerId.HasValue)
            allTasks = allTasks.Where(t => t.AssignedToUserId == workerId.Value);

        // filter by zone
        if (zoneId.HasValue)
            allTasks = allTasks.Where(t => t.ZoneId == zoneId.Value);

        // filter by status
        if (!string.IsNullOrEmpty(status) && Enum.TryParse<TaskStatus>(status, true, out var statusEnum))
            allTasks = allTasks.Where(t => t.Status == statusEnum);

        // filter by priority
        if (!string.IsNullOrEmpty(priority) && Enum.TryParse<FollowUp.Core.Enums.TaskPriority>(priority, true, out var priorityEnum))
            allTasks = allTasks.Where(t => t.Priority == priorityEnum);

        // paginate results
        var pagedTasks = allTasks
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            pagedTasks.Select(t => _mapper.Map<TaskResponse>(t))));
    }

    // update task details
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> UpdateTask(int id, [FromBody] UpdateTaskRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        // update only fields that were sent
        if (!string.IsNullOrEmpty(request.Title))
            task.Title = InputSanitizer.SanitizeString(request.Title, 200);

        if (!string.IsNullOrEmpty(request.Description))
            task.Description = InputSanitizer.SanitizeString(request.Description, 2000);

        // check if assigned user exist and is worker
        if (request.AssignedToUserId.HasValue)
        {
            var user = await _users.GetByIdAsync(request.AssignedToUserId.Value);
            if (user == null)
                return BadRequest(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));
            if (user.Role != Core.Enums.UserRole.Worker)
                return BadRequest(ApiResponse<object>.ErrorResponse("يمكن تعيين المهام للعمال فقط"));

            // check fair distribution if assigning to a different worker
            if (request.AssignedToUserId.Value != task.AssignedToUserId)
            {
                var workerActiveTasks = await GetActiveTaskCount(request.AssignedToUserId.Value);
                if (workerActiveTasks >= MaxActiveTasksPerWorker)
                {
                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        $"العامل لديه بالفعل {workerActiveTasks} مهام نشطة. الحد الأقصى هو {MaxActiveTasksPerWorker} مهام."));
                }
            }

            task.AssignedToUserId = request.AssignedToUserId.Value;
        }

        if (request.ZoneId.HasValue)
            task.ZoneId = request.ZoneId.Value;

        if (request.Priority.HasValue)
            task.Priority = request.Priority.Value;

        // check for conflicts when changing due date
        if (request.DueDate.HasValue)
        {
            var hasConflict = await HasTaskConflict(task.AssignedToUserId, request.DueDate.Value, task.TaskId);
            if (hasConflict)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "العامل لديه مهمة أخرى في نفس اليوم. يرجى اختيار تاريخ مختلف."));
            }
            task.DueDate = request.DueDate.Value;
        }

        if (request.Latitude.HasValue)
            task.Latitude = request.Latitude.Value;

        if (request.Longitude.HasValue)
            task.Longitude = request.Longitude.Value;

        if (!string.IsNullOrEmpty(request.LocationDescription))
            task.LocationDescription = InputSanitizer.SanitizeString(request.LocationDescription, 500);

        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} updated by supervisor", id);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    // update task status
    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        // workers can only change there own tasks
        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        // workers have limited status changes
        if (userRole == "Worker" &&
            request.Status != TaskStatus.InProgress &&
            request.Status != TaskStatus.Completed &&
            request.Status != TaskStatus.Pending)
        {
            return Forbid();
        }

        // update the status
        task.Status = request.Status;
        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        // set start time if task is started
        if (request.Status == TaskStatus.InProgress && !task.StartedAt.HasValue)
        {
            task.StartedAt = DateTime.UtcNow;
        }

        // set completion data if task is done
        if (request.Status == TaskStatus.Completed)
        {
            task.CompletedAt = DateTime.UtcNow;
            task.CompletionNotes = InputSanitizer.SanitizeString(request.CompletionNotes, 1000);
            task.Latitude = request.Latitude;
            task.Longitude = request.Longitude;
            task.PhotoUrl = request.PhotoUrl;
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    // complete task with photo upload
    [HttpPost("{id}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CompleteTaskWithPhoto(
        int id,
        [FromForm] CompleteTaskWithPhotoRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        // workers can only complete there own tasks
        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        // check if photo is required
        if (task.RequiresPhotoProof && request.Photo == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("الصورة مطلوبة لهذه المهمة"));
        }

        // validate gps coords
        if (request.Latitude.HasValue && request.Longitude.HasValue)
        {
            var validationResult = ValidateGpsCoordinates(request.Latitude.Value, request.Longitude.Value);
            if (validationResult != null)
                return BadRequest(ApiResponse<object>.ErrorResponse("إحداثيات GPS غير صالحة. يرجى التأكد من تفعيل الموقع"));

            // verify proof GPS location is within task's assigned zone (skip if geofencing disabled)
            var disableGeofencing = _config.GetValue<bool>("DeveloperMode:DisableGeofencing");
            if (!disableGeofencing && task.ZoneId.HasValue)
            {
                var zone = await _zones.GetByIdAsync(task.ZoneId.Value);
                if (zone?.Boundary != null)
                {
                    var proofPoint = NetTopologySuite.Geometries.GeometryFactory.Default.CreatePoint(
                        new NetTopologySuite.Geometries.Coordinate(request.Longitude.Value, request.Latitude.Value));

                    // allow some tolerance (worker may be near the zone boundary)
                    var isInsideOrNear = zone.Boundary.Contains(proofPoint) ||
                                         zone.Boundary.Distance(proofPoint) <= Core.Constants.GeofencingConstants.ProofLocationToleranceDegrees;

                    if (!isInsideOrNear)
                    {
                        _logger.LogWarning("Task {TaskId} completion rejected - proof location outside task zone. " +
                            "Proof location: ({Lat}, {Lon}), Zone: {ZoneName}",
                            id, request.Latitude.Value, request.Longitude.Value, zone.ZoneName);

                        return BadRequest(ApiResponse<object>.ErrorResponse(
                            "موقع الإثبات خارج منطقة المهمة. يرجى إرسال الإثبات من موقع المهمة."));
                    }
                }
            }
        }

        // Upload photo FIRST (before validation) so supervisor has evidence even if rejected
        string? photoUrl = null;
        if (request.Photo != null)
        {
            if (!_files.ValidateImage(request.Photo))
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة غير صالح"));

            photoUrl = await _files.UploadImageAsync(request.Photo, "tasks");
        }

        // Calculate distance from task location if task has specific coordinates
        int? completionDistanceMeters = null;
        bool isDistanceWarning = false;

        if (request.Latitude.HasValue && request.Longitude.HasValue && task.Latitude.HasValue && task.Longitude.HasValue)
        {
            completionDistanceMeters = CalculateDistanceMeters(
                task.Latitude.Value, task.Longitude.Value,
                request.Latitude.Value, request.Longitude.Value);

            // TWO-STRIKE SYSTEM: Give worker a chance to retry before rejection
            if (completionDistanceMeters > HardRejectDistanceMeters)
            {
                // Increment failed attempts
                task.FailedCompletionAttempts++;

                // FIRST ATTEMPT: Issue warning and keep task InProgress for retry
                if (task.FailedCompletionAttempts == 1)
                {
                    var warningMessage = $"⚠️ تحذير: الموقع غير مطابق لموقع المهمة. المسافة: {completionDistanceMeters} متر (الحد الأقصى: {HardRejectDistanceMeters} متر)";

                    // Keep task as InProgress so worker can retry
                    task.Status = TaskStatus.InProgress;
                    task.RejectionLatitude = request.Latitude;
                    task.RejectionLongitude = request.Longitude;
                    task.RejectionDistanceMeters = completionDistanceMeters;
                    task.PhotoUrl = photoUrl; // Save photo for supervisor review
                    task.SyncTime = DateTime.UtcNow;
                    task.SyncVersion++;

                    await _tasks.UpdateAsync(task);

                    // Save photo to Photos table if provided
                    if (photoUrl != null)
                    {
                        var photo = new Photo
                        {
                            PhotoUrl = photoUrl,
                            EntityType = "Task",
                            EntityId = task.TaskId,
                            OrderIndex = 0,
                            UploadedAt = DateTime.UtcNow,
                            UploadedByUserId = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _photos.AddAsync(photo);
                    }

                    await _tasks.SaveChangesAsync();

                    // Issue warning to worker
                    var worker = await _users.GetByIdAsync(userId!.Value);
                    if (worker != null)
                    {
                        worker.WarningCount++;
                        worker.LastWarningAt = DateTime.UtcNow;
                        worker.LastWarningReason = $"إرسال إثبات مهمة من موقع خاطئ (المسافة: {completionDistanceMeters}م)";
                        await _users.UpdateAsync(worker);
                        await _users.SaveChangesAsync();

                        // Notify worker about warning
                        try
                        {
                            await _notifications.SendWarningIssuedToWorkerAsync(
                                worker.UserId, worker.LastWarningReason, worker.WarningCount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send warning notification for task {TaskId}", task.TaskId);
                        }

                        // Alert supervisors
                        try
                        {
                            await _notifications.SendWarningAlertToSupervisorsAsync(
                                worker.UserId, worker.FullName, worker.LastWarningReason, worker.WarningCount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send supervisor warning alert for task {TaskId}", task.TaskId);
                        }
                    }

                    _logger.LogWarning("Task {TaskId} - FIRST ATTEMPT FAILED - worker {UserId} submitted from {Distance}m away (max: {MaxDistance}m). Keeping task InProgress for retry.",
                        id, userId, completionDistanceMeters, HardRejectDistanceMeters);

                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        $"{warningMessage}\n\n" +
                        $"💡 هذه المحاولة الأولى. لديك فرصة أخرى لإعادة المحاولة من الموقع الصحيح.\n" +
                        $"✅ تم حفظ الصورة التي أرسلتها.\n\n" +
                        $"يرجى التأكد من موقعك والمحاولة مرة أخرى."));
                }

                // SECOND ATTEMPT: Reject task and require supervisor intervention
                else
                {
                    var rejectionReason = $"الموقع غير مطابق لموقع المهمة (محاولتان فاشلتان). المسافة: {completionDistanceMeters} متر (الحد الأقصى: {HardRejectDistanceMeters} متر)";

                    // Reject task
                    task.Status = TaskStatus.Rejected;
                    task.IsAutoRejected = true;
                    task.RejectionReason = rejectionReason;
                    task.RejectedAt = DateTime.UtcNow;
                    task.RejectionLatitude = request.Latitude;
                    task.RejectionLongitude = request.Longitude;
                    task.RejectionDistanceMeters = completionDistanceMeters;
                    task.PhotoUrl = photoUrl; // Save photo for supervisor review
                    task.SyncTime = DateTime.UtcNow;
                    task.SyncVersion++;

                    await _tasks.UpdateAsync(task);

                    // Save photo to Photos table if provided (supervisor needs evidence)
                    if (photoUrl != null)
                    {
                        var photo = new Photo
                        {
                            PhotoUrl = photoUrl,
                            EntityType = "Task",
                            EntityId = task.TaskId,
                            OrderIndex = 0,
                            UploadedAt = DateTime.UtcNow,
                            UploadedByUserId = userId,
                            CreatedAt = DateTime.UtcNow
                        };
                        await _photos.AddAsync(photo);
                    }

                    await _tasks.SaveChangesAsync();

                    // Issue second warning to worker
                    var worker = await _users.GetByIdAsync(userId!.Value);
                    if (worker != null)
                    {
                        worker.WarningCount++;
                        worker.LastWarningAt = DateTime.UtcNow;
                        worker.LastWarningReason = $"إرسال إثبات مهمة من موقع خاطئ مرتين (المسافة: {completionDistanceMeters}م)";
                        await _users.UpdateAsync(worker);
                        await _users.SaveChangesAsync();

                        // Notify worker about rejection and warning
                        try
                        {
                            await _notifications.SendTaskAutoRejectedToWorkerAsync(
                                worker.UserId, task.TaskId, task.Title, rejectionReason, completionDistanceMeters.Value);
                            await _notifications.SendWarningIssuedToWorkerAsync(
                                worker.UserId, worker.LastWarningReason, worker.WarningCount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send auto-rejection notifications for task {TaskId}", task.TaskId);
                        }

                        // Alert supervisors
                        try
                        {
                            await _notifications.SendTaskAutoRejectedToSupervisorsAsync(
                                task.TaskId, task.Title, worker.FullName, rejectionReason, completionDistanceMeters.Value);
                            await _notifications.SendWarningAlertToSupervisorsAsync(
                                worker.UserId, worker.FullName, worker.LastWarningReason, worker.WarningCount);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to send supervisor alert for task {TaskId}", task.TaskId);
                        }
                    }

                    _logger.LogWarning("Task {TaskId} AUTO-REJECTED - worker {UserId} submitted from {Distance}m away twice (max: {MaxDistance}m)",
                        id, userId, completionDistanceMeters, HardRejectDistanceMeters);

                    return BadRequest(ApiResponse<object>.ErrorResponse(
                        $"⚠️ تم رفض الإثبات تلقائياً\n{rejectionReason}\n\n" +
                        $"📝 هذا الرفض تقني وليس عقوبة.\n" +
                        $"إذا كنت قد أنجزت المهمة فعلاً، سيقوم المشرف بمراجعة الصورة واتخاذ القرار المناسب.\n\n" +
                        $"✅ تم حفظ الصورة التي أرسلتها للمراجعة.\n" +
                        $"يرجى الاتصال بالمشرف للمتابعة."));
                }
            }

            // WARNING: If worker is between warning and hard reject thresholds
            if (completionDistanceMeters > WarningDistanceMeters)
            {
                isDistanceWarning = true;
                _logger.LogWarning("Task {TaskId} completed {Distance}m from task location (warning threshold: {WarningDistance}m)",
                    id, completionDistanceMeters, WarningDistanceMeters);
            }
        }

        // Photo was already uploaded earlier (before distance check) so supervisor has evidence

        try
        {
            // clean completion notes
            var sanitizedNotes = InputSanitizer.SanitizeString(request.CompletionNotes, 1000);

            // Add distance warning to notes if applicable
            if (isDistanceWarning && completionDistanceMeters.HasValue)
            {
                var warningNote = $"⚠️ تنبيه: تم الإنجاز على بعد {completionDistanceMeters}م من موقع المهمة";
                sanitizedNotes = string.IsNullOrEmpty(sanitizedNotes)
                    ? warningNote
                    : $"{warningNote}\n{sanitizedNotes}";
            }

            // update task with completion data
            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletionNotes = sanitizedNotes;
            task.Latitude = request.Latitude;
            task.Longitude = request.Longitude;
            task.PhotoUrl = photoUrl;
            task.CompletionDistanceMeters = completionDistanceMeters;
            task.IsDistanceWarning = isDistanceWarning;
            task.FailedCompletionAttempts = 0; // Reset failed attempts on successful completion
            task.SyncTime = DateTime.UtcNow;
            task.SyncVersion++;

            await _tasks.UpdateAsync(task);

            // save photo to photos table
            if (photoUrl != null)
            {
                var photo = new Photo
                {
                    PhotoUrl = photoUrl,
                    EntityType = "Task",
                    EntityId = task.TaskId,
                    OrderIndex = 0,
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await _photos.AddAsync(photo);

                // add to task's Photos collection so mapper includes it in response
                task.Photos.Add(photo);
            }

            // save task and photo together
            await _tasks.SaveChangesAsync();

            // notify supervisors that task was completed
            try
            {
                var worker = await _users.GetByIdAsync(userId!.Value);
                var workerName = worker?.FullName ?? "عامل";
                await _notifications.SendTaskCompletedToSupervisorsAsync(
                    task.TaskId,
                    task.Title,
                    workerName);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send task completion notification for task {TaskId}", task.TaskId);
            }
        }
        catch
        {
            // delete uploaded file if save failed
            if (photoUrl != null)
            {
                await _files.DeleteImageAsync(photoUrl);
            }
            throw;
        }

        _logger.LogInformation("Task {TaskId} completed by user {UserId} with photo {PhotoUrl}",
            id, userId, photoUrl);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    // get count of pending tasks for current user
    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var tasks = await _tasks.GetUserTasksAsync(userId.Value);
        var pendingCount = tasks.Count(t => t.Status == TaskStatus.Pending);

        return Ok(ApiResponse<int>.SuccessResponse(pendingCount));
    }

    // get overdue tasks
    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueTasks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        var userRole = GetCurrentUserRole();

        // fix pagination
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        // workers see only there overdue tasks
        IEnumerable<TaskEntity> tasks;
        if (userRole == "Worker")
        {
            tasks = await _tasks.GetOverdueTasksAsync(userId);
        }
        else
        {
            tasks = await _tasks.GetOverdueTasksAsync(null);
        }

        // paginate results
        var pagedTasks = tasks
            .OrderBy(t => t.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            pagedTasks.Select(t => _mapper.Map<TaskResponse>(t))));
    }

    // assign task to worker
    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> AssignTask(int id, [FromBody] AssignTaskRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));
        }

        // check if user exist and is worker
        var user = await _users.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));
        }

        if (user.Role != Core.Enums.UserRole.Worker)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن تعيين المهام للعمال فقط"));
        }

        // check fair distribution
        var workerActiveTasks = await GetActiveTaskCount(request.UserId);
        if (workerActiveTasks >= MaxActiveTasksPerWorker)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(
                $"العامل لديه بالفعل {workerActiveTasks} مهام نشطة. الحد الأقصى هو {MaxActiveTasksPerWorker} مهام."));
        }

        // check for time conflicts
        if (task.DueDate.HasValue)
        {
            var hasConflict = await HasTaskConflict(request.UserId, task.DueDate.Value, task.TaskId);
            if (hasConflict)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    "العامل لديه مهمة أخرى في نفس اليوم. يرجى اختيار تاريخ مختلف."));
            }
        }

        // assign task to user
        task.AssignedToUserId = request.UserId;
        task.Status = TaskStatus.Pending;

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        // send notification to newly assigned worker
        try
        {
            await _notifications.SendTaskAssignedNotificationAsync(
                request.UserId,
                task.TaskId,
                task.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send task assignment notification for task {TaskId}", task.TaskId);
        }

        _logger.LogInformation("Task {TaskId} assigned to user {UserId}", id, request.UserId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم تعيين المهمة بنجاح"));
    }

    // supervisor approve completed task
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ApproveTask(int id, [FromBody] ApproveTaskRequest? request)
    {
        // find task in db
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));
        }

        // can only approve completed tasks
        if (task.Status != TaskStatus.Completed)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن الموافقة على المهام المكتملة فقط"));
        }

        // update status and add supervisor notes
        task.Status = TaskStatus.Approved;
        if (!string.IsNullOrWhiteSpace(request?.Comments))
        {
            var sanitizedComments = InputSanitizer.SanitizeString(request.Comments, 500);
            task.CompletionNotes = task.CompletionNotes != null
                ? $"{task.CompletionNotes}\n\nملاحظات المشرف: {sanitizedComments}"
                : $"ملاحظات المشرف: {sanitizedComments}";
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        // notify worker that task was approved
        try
        {
            await _notifications.SendTaskUpdatedNotificationAsync(
                task.AssignedToUserId,
                task.TaskId,
                task.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send task approval notification for task {TaskId}", task.TaskId);
        }

        _logger.LogInformation("Task {TaskId} approved", id);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تمت الموافقة على المهمة بنجاح"));
    }

    // supervisor reject completed task
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> RejectTask(int id, [FromBody] RejectTaskRequest request)
    {
        // find task
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));
        }

        // can only reject completed tasks
        if (task.Status != TaskStatus.Completed)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن رفض المهام المكتملة فقط"));
        }

        // update status with rejection reason
        task.Status = TaskStatus.Rejected;
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            var sanitizedReason = InputSanitizer.SanitizeString(request.Reason, 500);
            task.CompletionNotes = task.CompletionNotes != null
                ? $"{task.CompletionNotes}\n\nسبب الرفض: {sanitizedReason}"
                : $"سبب الرفض: {sanitizedReason}";
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        // notify worker that task was rejected
        try
        {
            await _notifications.SendTaskUpdatedNotificationAsync(
                task.AssignedToUserId,
                task.TaskId,
                task.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send task rejection notification for task {TaskId}", task.TaskId);
        }

        _logger.LogInformation("Task {TaskId} rejected. Reason: {Reason}", id, request.Reason);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "تم رفض المهمة"));
    }

    // delete task
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> DeleteTask(int id)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        // delete photos from disk and db
        var photos = await _photos.GetPhotosByEntityAsync("Task", id);
        var photoUrls = photos.Select(p => p.PhotoUrl).Where(u => !string.IsNullOrEmpty(u)).ToList();
        if (photoUrls.Any())
        {
            await _files.DeleteImagesAsync(photoUrls!);
        }
        foreach (var photo in photos)
        {
            await _photos.DeleteAsync(photo);
        }

        await _tasks.DeleteAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} deleted with {PhotoCount} photos", id, photoUrls.Count);
        return NoContent();
    }

    // update task progress (for multi-day tasks)
    [HttpPut("{id}/progress")]
    [Authorize]
    public async Task<IActionResult> UpdateTaskProgress(int id, [FromBody] UpdateTaskProgressRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userId = GetCurrentUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<object>.ErrorResponse("رمز غير صالح"));

        var userRole = GetCurrentUserRole();

        // FIX 1: AUTHORIZATION - workers can only update their own tasks
        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        // FIX 2: STATUS VALIDATION - Only update tasks that are in progress or pending
        if (task.Status != TaskStatus.InProgress && task.Status != TaskStatus.Pending)
            return BadRequest(ApiResponse<object>.ErrorResponse("لا يمكن تحديث التقدم - المهمة ليست قيد التنفيذ"));

        // FIX 3: RATE LIMITING - Minimum 5 minutes between updates
        if (task.SyncTime.HasValue)
        {
            var timeSinceLastUpdate = DateTime.UtcNow - task.SyncTime.Value;
            if (timeSinceLastUpdate.TotalMinutes < 5)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"يرجى الانتظار {5 - (int)timeSinceLastUpdate.TotalMinutes} دقيقة قبل التحديث التالي"));
            }
        }

        // validate progress value
        if (request.ProgressPercentage < 0 || request.ProgressPercentage > 100)
            return BadRequest(ApiResponse<object>.ErrorResponse("نسبة التقدم يجب أن تكون بين 0 و 100"));

        // FIX 4: BACKWARDS PROGRESS WARNING - Log if progress decreased
        if (request.ProgressPercentage < task.ProgressPercentage)
        {
            _logger.LogWarning("Task {TaskId} progress decreased from {Old}% to {New}% by user {UserId}",
                id, task.ProgressPercentage, request.ProgressPercentage, userId);
        }

        // optional location verification for progress updates
        if (request.Latitude.HasValue && request.Longitude.HasValue && task.Latitude.HasValue && task.Longitude.HasValue)
        {
            var distance = CalculateDistanceMeters(
                task.Latitude.Value, task.Longitude.Value,
                request.Latitude.Value, request.Longitude.Value);

            if (distance > HardRejectDistanceMeters)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"لا يمكن تحديث التقدم من هذا الموقع. المسافة من موقع المهمة: {distance} متر"));
            }
        }

        // update progress
        task.ProgressPercentage = request.ProgressPercentage;
        task.ProgressNotes = InputSanitizer.SanitizeString(request.ProgressNotes, 1000);
        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        // handle deadline extension request (worker requests, supervisor approves via /extend endpoint)
        if (request.ExtendedDeadline.HasValue && request.ExtendedDeadline > task.DueDate)
        {
            task.ExtendedDeadline = request.ExtendedDeadline;
            task.ExtendedByUserId = userId; // Track who requested extension

            // notify supervisor about extension request
            if (task.AssignedByUserId.HasValue)
            {
                await _notifications.SendTaskExtensionRequestAsync(
                    task.AssignedByUserId.Value,
                    task.TaskId,
                    task.Title,
                    task.DueDate ?? DateTime.UtcNow,
                    request.ExtendedDeadline.Value);
            }
        }

        // auto-set status based on progress
        if (request.ProgressPercentage == 100)
        {
            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
        }
        else if (request.ProgressPercentage > 0 && task.Status == TaskStatus.Pending)
        {
            task.Status = TaskStatus.InProgress;
            if (!task.StartedAt.HasValue)
                task.StartedAt = DateTime.UtcNow;
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} progress updated to {Progress}% by user {UserId}",
            id, request.ProgressPercentage, userId);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(
            _mapper.Map<TaskResponse>(task),
            $"تم تحديث التقدم إلى {request.ProgressPercentage}%"));
    }

    // reassign task to different worker (supervisor/admin only)
    [HttpPut("{id}/reassign")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ReassignTask(int id, [FromBody] ReassignTaskRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var currentUserId = GetCurrentUserId();
        if (!currentUserId.HasValue)
            return Unauthorized();

        var currentUserRole = GetCurrentUserRole();

        // verify new worker exists
        var newWorker = await _users.GetByIdAsync(request.NewAssignedToUserId);
        if (newWorker == null)
            return NotFound(ApiResponse<object>.ErrorResponse("العامل الجديد غير موجود"));

        // verify new worker is actually a worker
        if (newWorker.Role != Core.Enums.UserRole.Worker)
            return BadRequest(ApiResponse<object>.ErrorResponse("المستخدم المحدد ليس عاملاً"));

        // verify new worker is active
        if (newWorker.Status != Core.Enums.UserStatus.Active)
            return BadRequest(ApiResponse<object>.ErrorResponse("العامل المحدد غير نشط"));

        // if supervisor, verify both workers belong to them
        if (currentUserRole == "Supervisor")
        {
            var oldWorker = await _users.GetByIdAsync(task.AssignedToUserId);
            if (oldWorker?.SupervisorId != currentUserId || newWorker.SupervisorId != currentUserId)
                return Forbid();
        }

        // don't allow reassignment if task is already completed
        if (task.Status == TaskStatus.Completed)
            return BadRequest(ApiResponse<object>.ErrorResponse("لا يمكن إعادة تعيين مهمة مكتملة"));

        var oldAssignedUserId = task.AssignedToUserId;
        var oldWorkerName = (await _users.GetByIdAsync(oldAssignedUserId))?.FullName ?? "غير معروف";

        // reassign task
        task.AssignedToUserId = request.NewAssignedToUserId;
        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        // if task was in progress, reset to pending since new worker hasn't started
        if (task.Status == TaskStatus.InProgress && !string.IsNullOrEmpty(request.ReassignmentReason))
        {
            task.Status = TaskStatus.Pending;
            task.StartedAt = null;
            task.ProgressPercentage = 0;
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} reassigned from user {OldUserId} to user {NewUserId} by {SupervisorId}. Reason: {Reason}",
            id, oldAssignedUserId, request.NewAssignedToUserId, currentUserId, request.ReassignmentReason ?? "غير محدد");

        // send notification to new worker
        try
        {
            await _notifications.SendTaskAssignedNotificationAsync(
                request.NewAssignedToUserId,
                task.TaskId,
                task.Title);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send notification to new worker");
        }

        return Ok(ApiResponse<object>.SuccessResponse(
            new
            {
                taskId = task.TaskId,
                title = task.Title,
                oldAssignedTo = oldWorkerName,
                newAssignedTo = newWorker.FullName,
                reason = request.ReassignmentReason
            },
            $"تم إعادة تعيين المهمة من {oldWorkerName} إلى {newWorker.FullName}"));
    }

    // extend task deadline (supervisor only)
    [HttpPut("{id}/extend")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ExtendTaskDeadline(int id, [FromBody] ExtendTaskDeadlineRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userId = GetCurrentUserId();

        // validate new deadline
        if (request.NewDeadline <= DateTime.UtcNow)
            return BadRequest(ApiResponse<object>.ErrorResponse("الموعد النهائي الجديد يجب أن يكون في المستقبل"));

        // store original deadline for logging
        var originalDeadline = task.DueDate;

        // update task
        task.ExtendedDeadline = request.NewDeadline;
        task.DueDate = request.NewDeadline;
        task.ExtendedByUserId = userId;

        // add extension note
        var extensionNote = $"تم تمديد الموعد النهائي بواسطة المشرف";
        if (!string.IsNullOrEmpty(request.Reason))
        {
            extensionNote += $": {InputSanitizer.SanitizeString(request.Reason, 500)}";
        }
        task.ProgressNotes = string.IsNullOrEmpty(task.ProgressNotes)
            ? extensionNote
            : $"{task.ProgressNotes}\n{extensionNote}";

        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        // notify worker about deadline extension
        try
        {
            await _notifications.SendTaskUpdatedNotificationAsync(
                task.AssignedToUserId, task.TaskId,
                $"تم تمديد موعد المهمة: {task.Title}");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send deadline extension notification for task {TaskId}", task.TaskId);
        }

        _logger.LogInformation("Task {TaskId} deadline extended from {OldDeadline} to {NewDeadline} by supervisor {UserId}",
            id, originalDeadline, request.NewDeadline, userId);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(
            _mapper.Map<TaskResponse>(task),
            "تم تمديد الموعد النهائي بنجاح"));
    }

    // get tasks with location warnings (supervisor view)
    [HttpGet("location-warnings")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetTasksWithLocationWarnings(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        var allTasks = await _tasks.GetAllAsync();

        // filter for tasks with distance warnings or auto-rejections
        var flaggedTasks = allTasks
            .Where(t => t.IsDistanceWarning || t.IsAutoRejected)
            .OrderByDescending(t => t.RejectedAt ?? t.CompletedAt ?? t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            flaggedTasks.Select(t => _mapper.Map<TaskResponse>(t))));
    }

    // reset rejected task (allow worker to retry)
    [HttpPut("{id}/reset")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ResetRejectedTask(int id)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        if (task.Status != TaskStatus.Rejected)
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن إعادة تعيين المهام المرفوضة فقط"));

        // reset task to in-progress so worker can retry
        task.Status = TaskStatus.InProgress;
        task.IsAutoRejected = false;
        task.RejectionReason = null;
        task.RejectedAt = null;
        task.RejectionLatitude = null;
        task.RejectionLongitude = null;
        task.RejectionDistanceMeters = null;
        task.FailedCompletionAttempts = 0; // Reset failed attempts counter
        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        // notify worker
        try
        {
            await _notifications.SendTaskUpdatedNotificationAsync(
                task.AssignedToUserId, task.TaskId,
                $"تم إعادة تعيين المهمة: {task.Title}. يمكنك إعادة المحاولة.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send task reset notification for task {TaskId}", task.TaskId);
        }

        _logger.LogInformation("Task {TaskId} reset to InProgress by supervisor", id);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(
            _mapper.Map<TaskResponse>(task),
            "تم إعادة تعيين المهمة. يمكن للعامل إعادة المحاولة."));
    }

    // approve rejected task (supervisor override - worker did the work but GPS was wrong)
    [HttpPut("{id}/approve-override")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ApproveRejectedTask(int id, [FromBody] ApproveOverrideRequest? request)
    {
        var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out var supervisorId))
            return Unauthorized(ApiResponse<object>.ErrorResponse("غير مصرح"));

        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        if (task.Status != TaskStatus.Rejected)
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن الموافقة على المهام المرفوضة فقط"));

        if (!task.IsAutoRejected)
            return BadRequest(ApiResponse<object>.ErrorResponse("هذه المهمة تم رفضها يدوياً. استخدم إعادة التعيين بدلاً من ذلك."));

        // Get supervisor info for logging
        var supervisor = await _users.GetByIdAsync(supervisorId);
        var supervisorName = supervisor?.FullName ?? "المشرف";

        // Approve task - change status to Completed
        task.Status = TaskStatus.Completed;
        task.CompletedAt = DateTime.UtcNow;

        // Add supervisor override note to completion notes
        var overrideNote = $"✅ تمت الموافقة من قبل المشرف: {supervisorName}\nالسبب: العامل أنجز المهمة فعلياً، المشكلة كانت تقنية (GPS)";

        if (!string.IsNullOrEmpty(request?.SupervisorNotes))
        {
            overrideNote += $"\nملاحظات المشرف: {request.SupervisorNotes}";
        }

        if (string.IsNullOrEmpty(task.CompletionNotes))
        {
            task.CompletionNotes = overrideNote;
        }
        else
        {
            task.CompletionNotes = $"{task.CompletionNotes}\n\n{overrideNote}";
        }

        // Keep rejection details for audit trail (don't clear them)
        // This helps track that it was originally rejected but then approved

        // DON'T reset FailedCompletionAttempts - keep history for reports
        // DON'T clear rejection details - keep for audit trail

        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        // Reduce worker warning count since this was a technical issue, not worker's fault
        var worker = await _users.GetByIdAsync(task.AssignedToUserId);
        if (worker != null && worker.WarningCount > 0)
        {
            // Reduce by the number of warnings issued for this task (max 2)
            var warningsToRemove = Math.Min(task.FailedCompletionAttempts, worker.WarningCount);
            worker.WarningCount -= warningsToRemove;
            await _users.UpdateAsync(worker);
            await _users.SaveChangesAsync();

            _logger.LogInformation("Reduced worker {WorkerId} warning count by {Count} after supervisor override approval",
                worker.UserId, warningsToRemove);
        }

        // Notify worker about approval
        try
        {
            await _notifications.SendTaskUpdatedNotificationAsync(
                task.AssignedToUserId, task.TaskId,
                $"✅ تمت الموافقة على إنجاز المهمة: {task.Title}\n\nقام المشرف بمراجعة الصورة والموافقة على إنجازك. شكراً على جهودك!");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send task approval notification for task {TaskId}", task.TaskId);
        }

        _logger.LogInformation("Task {TaskId} approved by supervisor {SupervisorId} ({SupervisorName}) - was auto-rejected but worker did the work (GPS issue)",
            id, supervisorId, supervisorName);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(
            _mapper.Map<TaskResponse>(task),
            "تمت الموافقة على المهمة. تم تقليل عدد التحذيرات للعامل."));
    }

    // helper: count active tasks for a worker
    private async Task<int> GetActiveTaskCount(int workerId)
    {
        var workerTasks = await _tasks.GetUserTasksAsync(workerId);
        return workerTasks.Count(t => t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress);
    }

    // helper: check if worker has another task on the same day
    private async Task<bool> HasTaskConflict(int workerId, DateTime dueDate, int? excludeTaskId)
    {
        var workerTasks = await _tasks.GetUserTasksAsync(workerId);
        return workerTasks.Any(t =>
            t.DueDate.HasValue &&
            t.DueDate.Value.Date == dueDate.Date &&
            (t.Status == TaskStatus.Pending || t.Status == TaskStatus.InProgress) &&
            (!excludeTaskId.HasValue || t.TaskId != excludeTaskId.Value));
    }

    // helper: calculate distance between two GPS coordinates in meters using Haversine formula
    private static int CalculateDistanceMeters(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadiusMeters = 6371000;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (int)(earthRadiusMeters * c);
    }

    private static double ToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}
