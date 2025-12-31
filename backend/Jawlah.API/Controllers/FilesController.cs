using System.Security.Claims;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // require authentication for all file access
public class FilesController : ControllerBase
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<FilesController> _logger;
    private readonly IPhotoRepository _photos;
    private readonly ITaskRepository _tasks;
    private readonly IIssueRepository _issues;
    private const string SecureStorageFolder = "Storage";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };

    public FilesController(
        IWebHostEnvironment env,
        ILogger<FilesController> logger,
        IPhotoRepository photos,
        ITaskRepository tasks,
        IIssueRepository issues)
    {
        _env = env;
        _logger = logger;
        _photos = photos;
        _tasks = tasks;
        _issues = issues;
    }

    [HttpGet("{folder}/{filename}")]
    public async Task<IActionResult> GetFile(string folder, string filename)
    {
        try
        {
            // check if the folder name and file name are correct
            if (string.IsNullOrWhiteSpace(folder) || !System.Text.RegularExpressions.Regex.IsMatch(folder, "^[a-zA-Z0-9_-]+$") ||
                string.IsNullOrWhiteSpace(filename) || filename.Contains("..") || filename.Contains("/") || filename.Contains("\\") || 
                !System.Text.RegularExpressions.Regex.IsMatch(filename, "^[a-zA-Z0-9_.-]+$"))
            {
                _logger.LogWarning("Invalid folder or filename requested: {Folder}/{Filename}", folder, filename);
                return BadRequest(new { error = "مسار الملف غير صالح" });
            }

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            // if the user is a worker, they should only see their own photos
            if (userRole != "Admin" && userRole != "Supervisor")
            {
                if (string.IsNullOrEmpty(userIdStr) || !int.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized();
                }

                // find the photo in the database
                var photos = await _photos.GetAllAsync();
                var photo = photos.FirstOrDefault(p => p.PhotoUrl.Contains(filename));

                if (photo == null)
                {
                    _logger.LogWarning("Photo with filename {Filename} not found in database", filename);
                    return NotFound(new { error = "الملف غير موجود" });
                }

                // check if the photo belongs to the worker's tasks or issues
                bool hasAccess = false;
                if (photo.EntityType == "Task")
                {
                    var task = await _tasks.GetByIdAsync(photo.EntityId);
                    if (task != null && task.AssignedToUserId == userId)
                    {
                        hasAccess = true;
                    }
                }
                else if (photo.EntityType == "Issue")
                {
                    var issue = await _issues.GetByIdAsync(photo.EntityId);
                    if (issue != null && issue.ReportedByUserId == userId)
                    {
                        hasAccess = true;
                    }
                }

                if (!hasAccess)
                {
                    _logger.LogWarning("User {UserId} attempted to access unauthorized file: {PhotoUrl}", userId, photo.PhotoUrl);
                    return Forbid();
                }
            }

            // check the file extension
            var extension = Path.GetExtension(filename).ToLowerInvariant();
            if (!AllowedExtensions.Contains(extension))
            {
                _logger.LogWarning("Disallowed file extension requested: {Extension}", extension);
                return BadRequest(new { error = "نوع الملف غير مسموح" });
            }

            // get the full path of the file on the server
            var filePath = Path.Combine(
                _env.ContentRootPath,
                SecureStorageFolder,
                "uploads",
                folder,
                filename
            );

            // security check for path traversal
            var fullPath = Path.GetFullPath(filePath);
            var secureBasePath = Path.GetFullPath(Path.Combine(_env.ContentRootPath, SecureStorageFolder, "uploads"));

            if (!fullPath.StartsWith(secureBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Path traversal attempt detected: {FilePath}", filePath);
                return BadRequest(new { error = "مسار الملف غير صالح" });
            }

            if (!System.IO.File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return NotFound(new { error = "الملف غير موجود" });
            }

            // detect content type to return the file correctly
            var contentType = extension switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                _ => "application/octet-stream"
            };

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
