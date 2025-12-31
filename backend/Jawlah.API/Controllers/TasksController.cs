using System.Security.Claims;
using AutoMapper;
using Jawlah.API.Models;
using Jawlah.API.Utils;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Tasks;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskEntity = Jawlah.Core.Entities.Task;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class TasksController : BaseApiController
{
    private readonly ITaskRepository _tasks;
    private readonly IPhotoRepository _photos;
    private readonly IUserRepository _users;
    private readonly ILogger<TasksController> _logger;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public TasksController(
        ITaskRepository tasks,
        IPhotoRepository photos,
        IUserRepository users,
        ILogger<TasksController> logger,
        IFileStorageService files,
        IMapper mapper)
    {
        _tasks = tasks;
        _photos = photos;
        _users = users;
        _logger = logger;
        _files = files;
        _mapper = mapper;
    }

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

        // validate pagination parameters
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        // parse status filter if provided
        TaskStatus? statusEnum = null;
        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<TaskStatus>(status, true, out var parsedStatus))
            {
                statusEnum = parsedStatus;
            }
        }

        // parse priority filter if provided
        Jawlah.Core.Enums.TaskPriority? priorityEnum = null;
        if (!string.IsNullOrEmpty(priority))
        {
            if (Enum.TryParse<Jawlah.Core.Enums.TaskPriority>(priority, true, out var parsedPriority))
            {
                priorityEnum = parsedPriority;
            }
        }

        var tasks = await _tasks.GetUserTasksAsync(userId.Value, statusEnum, priorityEnum, page, pageSize);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            tasks.Select(t => _mapper.Map<TaskResponse>(t))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(int id)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // sanitize inputs to prevent XSS attacks
        var sanitizedTitle = InputSanitizer.SanitizeString(request.Title, 200);
        var sanitizedDescription = InputSanitizer.SanitizeString(request.Description, 2000);
        var sanitizedLocation = InputSanitizer.SanitizeString(request.LocationDescription, 500);

        var task = new TaskEntity
        {
            Title = sanitizedTitle,
            Description = sanitizedDescription,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = userId.Value,
            ZoneId = request.ZoneId,
            Priority = request.Priority,
            Status = TaskStatus.Pending,
            TaskType = request.TaskType,
            RequiresPhotoProof = request.RequiresPhotoProof,
            EstimatedDurationMinutes = request.EstimatedDurationMinutes,
            DueDate = request.DueDate,
            LocationDescription = sanitizedLocation,
            CreatedAt = DateTime.UtcNow,
            EventTime = DateTime.UtcNow,
            SyncTime = DateTime.UtcNow,
            IsSynced = true,
            SyncVersion = 1
        };

        await _tasks.AddAsync(task);
        await _tasks.SaveChangesAsync();

        return CreatedAtAction(nameof(GetTaskById), new { id = task.TaskId },
            ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المهمة غير موجودة"));

        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        if (userRole == "Worker" &&
            request.Status != TaskStatus.InProgress &&
            request.Status != TaskStatus.Completed &&
            request.Status != TaskStatus.Pending)
        {
            return Forbid();
        }

        // update task status
        task.Status = request.Status;
        task.SyncTime = DateTime.UtcNow;
        task.SyncVersion++;

        if (request.Status == TaskStatus.InProgress && !task.StartedAt.HasValue)
        {
            task.StartedAt = DateTime.UtcNow;
        }

        if (request.Status == TaskStatus.Completed)
        {
            task.CompletedAt = DateTime.UtcNow;
            task.CompletionNotes = request.CompletionNotes;
            task.Latitude = request.Latitude;
            task.Longitude = request.Longitude;
            task.PhotoUrl = request.PhotoUrl;
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(_mapper.Map<TaskResponse>(task)));
    }

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
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        if (task.RequiresPhotoProof && request.Photo == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("الصورة مطلوبة لهذه المهمة"));
        }

        string? photoUrl = null;
        if (request.Photo != null)
        {
            if (!_files.ValidateImage(request.Photo))
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة غير صالح"));

            photoUrl = await _files.UploadImageAsync(request.Photo, "tasks");
        }

        try
        {
            // sanitize completion notes
            var sanitizedNotes = InputSanitizer.SanitizeString(request.CompletionNotes, 1000);

            // update task
            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletionNotes = sanitizedNotes;
            task.Latitude = request.Latitude;
            task.Longitude = request.Longitude;
            task.PhotoUrl = photoUrl; // keep for backward compatibility
            task.SyncTime = DateTime.UtcNow;
            task.SyncVersion++;

            await _tasks.UpdateAsync(task);
            await _tasks.SaveChangesAsync();

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
                await _tasks.SaveChangesAsync();
            }
        }
        catch
        {
            // cleanup uploaded file if database save failed
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
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        // validate pagination
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 100) pageSize = 50;

        // get overdue tasks based on role
        IEnumerable<TaskEntity> tasks;
        if (userRole == "Worker")
        {
            tasks = await _tasks.GetOverdueTasksAsync(userId);
        }
        else
        {
            tasks = await _tasks.GetOverdueTasksAsync(null);
        }

        // apply pagination
        var pagedTasks = tasks
            .OrderBy(t => t.DueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            pagedTasks.Select(t => _mapper.Map<TaskResponse>(t))));
    }

    [HttpPost("{id}/assign")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> AssignTask(int id, [FromBody] AssignTaskRequest request)
    {
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));
        }

        // verify the assigned user exists
        var user = await _users.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));
        }

        task.AssignedToUserId = request.UserId;
        task.Status = TaskStatus.Pending; // Reset status when reassigning

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} assigned to user {UserId}", id, request.UserId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Task assigned successfully"));
    }

    // optional: supervisor can approve completed tasks
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ApproveTask(int id, [FromBody] ApproveTaskRequest? request)
    {
        // find the task
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));
        }

        // can only approve completed tasks
        if (task.Status != TaskStatus.Completed)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن الموافقة على المهام المكتملة فقط"));
        }

        // update status and add notes
        task.Status = TaskStatus.Approved;
        if (request?.Comments != null)
        {
            task.CompletionNotes = task.CompletionNotes != null
                ? $"{task.CompletionNotes}\n\nSupervisor Comments: {request.Comments}"
                : $"Supervisor Comments: {request.Comments}";
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} approved", id);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Task approved successfully"));
    }

    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> RejectTask(int id, [FromBody] RejectTaskRequest request)
    {
        // find the task
        var task = await _tasks.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));
        }

        // check status
        if (task.Status != TaskStatus.Completed)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("يمكن رفض المهام المكتملة فقط"));
        }

        // update status with reason
        task.Status = TaskStatus.Rejected;
        if (request.Reason != null)
        {
            task.CompletionNotes = task.CompletionNotes != null
                ? $"{task.CompletionNotes}\n\nRejection Reason: {request.Reason}"
                : $"Rejection Reason: {request.Reason}";
        }

        await _tasks.UpdateAsync(task);
        await _tasks.SaveChangesAsync();

        _logger.LogInformation($"Task {{TaskId}} rejected. Reason: {request.Reason}", id);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Task rejected successfully"));
    }

}
