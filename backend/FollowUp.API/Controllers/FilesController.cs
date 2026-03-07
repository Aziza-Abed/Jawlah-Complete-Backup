using FollowUp.Core.DTOs.Common;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Files")]
public class FilesController : BaseApiController
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;
    private readonly IPhotoRepository _photos;
    private readonly ITaskRepository _tasks;
    private readonly IIssueRepository _issues;
    private readonly IAppealRepository _appeals;
    private readonly IUserRepository _users;
    private readonly IAuditLogService _audit;
    private const string SecureStorageFolder = "Storage";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

    public FilesController(
        IWebHostEnvironment env,
        ILogger<FilesController> logger,
        IPhotoRepository photos,
        ITaskRepository tasks,
        IIssueRepository issues,
        IAppealRepository appeals,
        IUserRepository users,
        IAuditLogService audit)
    {
        _env = env;
        _logger = logger;
        _photos = photos;
        _tasks = tasks;
        _issues = issues;
        _appeals = appeals;
        _users = users;
        _audit = audit;
    }

    [HttpGet("{folder}/{filename}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "serve an uploaded file")]
    public async Task<IActionResult> GetFile(string folder, string filename)
    {
        try
        {
            // validate folder and filename are safe
            if (string.IsNullOrWhiteSpace(folder) || !System.Text.RegularExpressions.Regex.IsMatch(folder, "^[a-zA-Z0-9_-]+$") ||
                string.IsNullOrWhiteSpace(filename) || filename.Contains("..") || filename.Contains("/") || filename.Contains("\\") ||
                !System.Text.RegularExpressions.Regex.IsMatch(filename, "^[a-zA-Z0-9_.-]+$"))
            {
                _logger.LogWarning("Invalid folder or filename requested: {Folder}/{Filename}", folder, filename);
                return BadRequest(ApiResponse<object>.ErrorResponse("مسار الملف غير صالح"));
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            // Profile photos are accessible to any authenticated user (no ownership check needed)
            // With [AllowAnonymous], browser <img> tags can also load them directly
            bool isProfilePhoto = folder.Equals("profiles", StringComparison.OrdinalIgnoreCase);

            // Admins can access all files; profile photos skip ownership checks
            if (userRole != "Admin" && !isProfilePhoto)
            {
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                bool hasAccess = false;

                // appeal photos are stored in Appeal entity (not Photo table)
                if (folder.Equals("appeals", StringComparison.OrdinalIgnoreCase))
                {
                    var appeal = await _appeals.GetByEvidencePhotoFilenameAsync(filename);
                    if (appeal == null)
                        return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود"));

                    hasAccess = appeal.UserId == userId.Value
                        || (userRole == "Supervisor" && await IsSupervisorOfAsync(userId.Value, appeal.UserId));
                }
                else
                {
                    // task/issue photos are in Photo table
                    var photo = await _photos.GetByFilenameAsync(filename);
                    if (photo == null)
                    {
                        _logger.LogWarning("Photo with filename {Filename} not found in database", filename);
                        return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود"));
                    }

                    if (photo.EntityType == "Task")
                    {
                        var task = await _tasks.GetByIdAsync(photo.EntityId);
                        if (task != null)
                        {
                            if (task.IsTeamTask && task.TeamId.HasValue)
                            {
                                // For team tasks: worker must be in the team, supervisor must manage a team member
                                var requestingUser = await _users.GetByIdAsync(userId.Value);
                                hasAccess = requestingUser?.TeamId == task.TeamId
                                    || (userRole == "Supervisor" && await IsSupervisorOfTeamAsync(userId.Value, task.TeamId.Value));
                            }
                            else
                            {
                                hasAccess = task.AssignedToUserId == userId.Value
                                    || (userRole == "Supervisor" && await IsSupervisorOfAsync(userId.Value, task.AssignedToUserId));
                            }
                        }
                    }
                    else if (photo.EntityType == "Issue")
                    {
                        var issue = await _issues.GetByIdAsync(photo.EntityId);
                        if (issue != null)
                            hasAccess = issue.ReportedByUserId == userId.Value
                                || (userRole == "Supervisor" && await IsSupervisorOfAsync(userId.Value, issue.ReportedByUserId));
                    }
                }

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} ({Role}) attempted to access unauthorized file in {Folder}", userId.Value, userRole, folder);
                    return Forbid();
                }
            }

            // check extension is allowed
            var extension = Path.GetExtension(filename).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Disallowed file extension requested: {Extension}", extension);
                return BadRequest(ApiResponse<object>.ErrorResponse("نوع الملف غير مسموح"));
            }

            // build full file path
            var filePath = Path.Combine(
                _env.ContentRootPath,
                SecureStorageFolder,
                "uploads",
                folder,
                filename
            );

            // check for path traversal attack
            var fullPath = Path.GetFullPath(filePath);
            var secureBasePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, SecureStorageFolder, "uploads"));

            if (!fullPath.StartsWith(secureBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                return BadRequest(ApiResponse<object>.ErrorResponse("مسار الملف غير صالح"));
            }

            // check file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود"));
            }

            // get content type based on extension
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

            // audit log for file access
            await _audit.LogAsync(
                userId,
                null,
                "FileAccess",
                $"ملف: {folder}/{filename}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());

            // prevent caching of sensitive photos by browsers and proxies
            Response.Headers["Cache-Control"] = "private, no-store";
            Response.Headers["Pragma"] = "no-cache";

            var fileStream = System.IO.File.OpenRead(filePath);

            return File(fileStream, contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving file: {Folder}/{Filename}", folder, filename);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("خطأ داخلي في الخادم"));
        }
    }

    // check if a supervisor manages the given worker
    private async Task<bool> IsSupervisorOfAsync(int supervisorId, int workerId)
    {
        var worker = await _users.GetByIdAsync(workerId);
        return worker != null && worker.SupervisorId == supervisorId;
    }

    // check if a supervisor manages at least one member of the given team
    private async Task<bool> IsSupervisorOfTeamAsync(int supervisorId, int teamId)
    {
        var myWorkers = await _users.GetWorkersBySupervisorAsync(supervisorId);
        return myWorkers.Any(w => w.TeamId == teamId);
    }
}
