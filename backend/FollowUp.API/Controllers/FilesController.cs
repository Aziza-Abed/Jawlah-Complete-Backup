using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

// this controller serve uploaded files securely
[Route("api/[controller]")]
[Authorize]
public class FilesController : BaseApiController
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;
    private readonly IPhotoRepository _photos;
    private readonly ITaskRepository _tasks;
    private readonly IIssueRepository _issues;
    private readonly IAppealRepository _appeals;
    private readonly IUserRepository _users;
    private readonly AuditLogService _audit;
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
        AuditLogService audit)
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

    // get file by folder and filename
    [HttpGet("{folder}/{filename}")]
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
                return BadRequest(new { error = "مسار الملف غير صالح" });
            }

            var userRole = GetCurrentUserRole();
            var userId = GetCurrentUserId();

            // Admins can access all files
            if (userRole != "Admin")
            {
                if (!userId.HasValue)
                {
                    return Unauthorized();
                }

                bool hasAccess = false;

                // Appeal photos are stored in Appeal entity (not Photo table)
                if (folder.Equals("appeals", StringComparison.OrdinalIgnoreCase))
                {
                    var appeal = await _appeals.GetByEvidencePhotoFilenameAsync(filename);

                    if (appeal == null)
                    {
                        return NotFound(new { error = "الملف غير موجود" });
                    }

                    // Worker: can only see own appeal photos
                    if (appeal.UserId == userId.Value)
                    {
                        hasAccess = true;
                    }
                    // Supervisor: can see appeal photos from workers under their supervision
                    else if (userRole == "Supervisor")
                    {
                        var worker = await _users.GetByIdAsync(appeal.UserId);
                        if (worker != null && worker.SupervisorId == userId.Value)
                        {
                            hasAccess = true;
                        }
                    }
                }
                else
                {
                    // Task/Issue photos are in Photo table
                    var photo = await _photos.GetByFilenameAsync(filename);

                    if (photo == null)
                    {
                        _logger.LogWarning("Photo with filename {Filename} not found in database", filename);
                        return NotFound(new { error = "الملف غير موجود" });
                    }

                    if (photo.EntityType == "Task")
                    {
                        var task = await _tasks.GetByIdAsync(photo.EntityId);
                        if (task != null)
                        {
                            // Worker: can only see own tasks
                            if (task.AssignedToUserId == userId.Value)
                            {
                                hasAccess = true;
                            }
                            // Supervisor: can see tasks of workers under their supervision
                            else if (userRole == "Supervisor")
                            {
                                var worker = await _users.GetByIdAsync(task.AssignedToUserId);
                                if (worker != null && worker.SupervisorId == userId.Value)
                                {
                                    hasAccess = true;
                                }
                            }
                        }
                    }
                    else if (photo.EntityType == "Issue")
                    {
                        var issue = await _issues.GetByIdAsync(photo.EntityId);
                        if (issue != null)
                        {
                            // Worker: can only see own issues
                            if (issue.ReportedByUserId == userId.Value)
                            {
                                hasAccess = true;
                            }
                            // Supervisor: can see issues from workers under their supervision
                            else if (userRole == "Supervisor")
                            {
                                var worker = await _users.GetByIdAsync(issue.ReportedByUserId);
                                if (worker != null && worker.SupervisorId == userId.Value)
                                {
                                    hasAccess = true;
                                }
                            }
                        }
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
                return BadRequest(new { error = "نوع الملف غير مسموح" });
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
                return BadRequest(new { error = "مسار الملف غير صالح" });
            }

            // check file exists
            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return NotFound(new { error = "الملف غير موجود" });
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

            // return file
            var fileStream = System.IO.File.OpenRead(filePath);

            return File(fileStream, contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving file: {Folder}/{Filename}", folder, filename);
            return StatusCode(500, new { error = "خطأ داخلي في الخادم" });
        }
    }
}
