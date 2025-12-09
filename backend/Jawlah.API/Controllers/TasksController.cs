using System.Security.Claims;
using Jawlah.API.Models;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Tasks;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskEntity = Jawlah.Core.Entities.Task;
using TaskStatus = Jawlah.Core.Enums.TaskStatus;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class TasksController : BaseApiController
{
    private readonly ITaskRepository _taskRepo;
    private readonly IUserRepository _userRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<TasksController> _logger;
    private readonly IFileStorageService _fileStorageService;

    public TasksController(
        ITaskRepository taskRepo,
        IUserRepository userRepo,
        JawlahDbContext context,
        ILogger<TasksController> logger,
        IFileStorageService fileStorageService)
    {
        _taskRepo = taskRepo;
        _userRepo = userRepo;
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    [HttpGet("my-tasks")]
    public async Task<IActionResult> GetMyTasks()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var tasks = await _taskRepo.GetUserTasksAsync(userId.Value);
        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            tasks.Select(t => MapToTaskResponse(t))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetTaskById(int id)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));

        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(task)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> CreateTask([FromBody] CreateTaskRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var task = new TaskEntity
        {
            Title = request.Title,
            Description = request.Description,
            AssignedToUserId = request.AssignedToUserId,
            AssignedByUserId = userId.Value,
            ZoneId = request.ZoneId,
            Priority = request.Priority,
            Status = TaskStatus.Pending,
            DueDate = request.DueDate,
            LocationDescription = request.LocationDescription,
            CreatedAt = DateTime.UtcNow,
            EventTime = DateTime.UtcNow,
            SyncTime = DateTime.UtcNow,
            IsSynced = true,
            SyncVersion = 1
        };

        await _taskRepo.AddAsync(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} created by user {UserId}", task.TaskId, userId);

        return CreatedAtAction(nameof(GetTaskById), new { id = task.TaskId },
            ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(task)));
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateTaskStatus(int id, [FromBody] UpdateTaskStatusRequest request)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));

        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        // Workers can only change status to InProgress or Completed
        // Cancelled, Approved, Rejected are supervisor/admin only
        if (userRole == "Worker" && 
            request.Status != TaskStatus.InProgress && 
            request.Status != TaskStatus.Completed &&
            request.Status != TaskStatus.Pending)
        {
            return Forbid();
        }

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

        await _taskRepo.UpdateAsync(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} status updated to {Status} by user {UserId}",
            id, request.Status, userId);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(task)));
    }

    [HttpPost("{id}/complete")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> CompleteTaskWithPhoto(
        int id,
        [FromForm] CompleteTaskWithPhotoRequest request)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));

        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        if (userRole == "Worker" && task.AssignedToUserId != userId)
            return Forbid();

        // Upload photo if provided
        string? photoUrl = null;
        if (request.Photo != null)
        {
            if (!_fileStorageService.ValidateImage(request.Photo))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid image file"));

            photoUrl = await _fileStorageService.UploadImageAsync(request.Photo, "tasks");
        }

        // Cleanup uploaded file if database save fails
        try
        {
            // Update task
            task.Status = TaskStatus.Completed;
            task.CompletedAt = DateTime.UtcNow;
            task.CompletionNotes = request.CompletionNotes;
            task.Latitude = request.Latitude;
            task.Longitude = request.Longitude;
            task.PhotoUrl = photoUrl;
            task.SyncTime = DateTime.UtcNow;
            task.SyncVersion++;

            await _taskRepo.UpdateAsync(task);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Cleanup uploaded file if database save failed
            if (photoUrl != null)
            {
                try
                {
                    await _fileStorageService.DeleteImageAsync(photoUrl);
                    _logger.LogWarning("Cleaned up orphaned photo file: {PhotoUrl}", photoUrl);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup orphaned photo: {PhotoUrl}", photoUrl);
                }
            }
            throw; // Re-throw original exception
        }

        _logger.LogInformation("Task {TaskId} completed by user {UserId} with photo {PhotoUrl}",
            id, userId, photoUrl);

        return Ok(ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(task)));
    }

    [HttpGet("pending-count")]
    public async Task<IActionResult> GetPendingCount()
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var tasks = await _taskRepo.GetUserTasksAsync(userId.Value);
        var pendingCount = tasks.Count(t => t.Status == TaskStatus.Pending);

        return Ok(ApiResponse<int>.SuccessResponse(pendingCount));
    }

    [HttpGet("overdue")]
    public async Task<IActionResult> GetOverdueTasks()
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        IEnumerable<TaskEntity> tasks;

        if (userRole == "Worker")
        {
            var allTasks = await _taskRepo.GetOverdueTasksAsync();
            tasks = allTasks.Where(t => t.AssignedToUserId == userId);
        }
        else
        {
            tasks = await _taskRepo.GetOverdueTasksAsync();
        }

        return Ok(ApiResponse<IEnumerable<TaskResponse>>.SuccessResponse(
            tasks.Select(t => MapToTaskResponse(t))));
    }

    [HttpPost("{id}/assign")]
    public async Task<IActionResult> AssignTask(int id, [FromBody] AssignTaskRequest request)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));
        }

        // Verify the assigned user exists
        var user = await _userRepo.GetByIdAsync(request.UserId);
        if (user == null)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("User not found"));
        }

        task.AssignedToUserId = request.UserId;
        task.Status = TaskStatus.Pending; // Reset status when reassigning

        await _taskRepo.UpdateAsync(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} assigned to user {UserId}", id, request.UserId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Task assigned successfully"));
    }

    // Note: This feature is optional
    // Default flow: Worker completes task → Status = Completed → Task is considered done
    // This endpoint is for EXCEPTION HANDLING only (supervisor validation if needed)
    [HttpPut("{id}/approve")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ApproveTask(int id, [FromBody] ApproveTaskRequest? request)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));
        }

        if (task.Status != TaskStatus.Completed)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Only completed tasks can be approved"));
        }

        task.Status = TaskStatus.Approved;
        if (request?.Comments != null)
        {
            task.CompletionNotes = task.CompletionNotes != null
                ? $"{task.CompletionNotes}\n\nSupervisor Comments: {request.Comments}"
                : $"Supervisor Comments: {request.Comments}";
        }

        await _taskRepo.UpdateAsync(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} approved", id);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Task approved successfully"));
    }

    // Note: This feature is optional
    // Supervisor can reject a completed task if there's a problem with the work
    // This is NOT part of the mandatory workflow - tasks marked Completed are considered done
    [HttpPut("{id}/reject")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> RejectTask(int id, [FromBody] RejectTaskRequest request)
    {
        var task = await _taskRepo.GetByIdAsync(id);
        if (task == null)
        {
            return NotFound(ApiResponse<object>.ErrorResponse("Task not found"));
        }

        if (task.Status != TaskStatus.Completed)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Only completed tasks can be rejected"));
        }

        task.Status = TaskStatus.Rejected;
        task.CompletionNotes = task.CompletionNotes != null
            ? $"{task.CompletionNotes}\n\nRejection Reason: {request.Reason}"
            : $"Rejection Reason: {request.Reason}";

        await _taskRepo.UpdateAsync(task);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Task {TaskId} rejected. Reason: {Reason}", id, request.Reason);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Task rejected successfully"));
    }

    private TaskResponse MapToTaskResponse(TaskEntity task)
    {
        return new TaskResponse
        {
            TaskId = task.TaskId,
            Title = task.Title,
            Description = task.Description,
            Priority = task.Priority,
            Status = task.Status,
            AssignedToUserId = task.AssignedToUserId,
            AssignedByUserId = task.AssignedByUserId,
            ZoneId = task.ZoneId,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            StartedAt = task.StartedAt,
            CompletedAt = task.CompletedAt,
            LocationDescription = task.LocationDescription,
            CompletionNotes = task.CompletionNotes,
            PhotoUrl = task.PhotoUrl,
            Latitude = task.Latitude,
            Longitude = task.Longitude
        };
    }
}
