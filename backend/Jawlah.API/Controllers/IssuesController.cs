using System.Security.Claims;
using Jawlah.API.Models;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Issues;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class IssuesController : BaseApiController
{
    private readonly IIssueRepository _issueRepo;
    private readonly JawlahDbContext _context;
    private readonly ILogger<IssuesController> _logger;
    private readonly IFileStorageService _fileStorageService;

    public IssuesController(
        IIssueRepository issueRepo,
        JawlahDbContext context,
        ILogger<IssuesController> logger,
        IFileStorageService fileStorageService)
    {
        _issueRepo = issueRepo;
        _context = context;
        _logger = logger;
        _fileStorageService = fileStorageService;
    }

    [HttpPost("report")]
    public async Task<IActionResult> ReportIssue([FromBody] ReportIssueRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var issue = new Issue
        {
            Title = request.Title,
            Description = request.Description,
            Type = request.Type,
            Severity = request.Severity,
            ReportedByUserId = userId.Value,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationDescription = request.LocationDescription,
            PhotoUrl = request.PhotoUrl,
            Status = IssueStatus.Reported,
            ReportedAt = DateTime.UtcNow,
            EventTime = DateTime.UtcNow,
            SyncTime = DateTime.UtcNow,
            IsSynced = true,
            SyncVersion = 1
        };

        await _issueRepo.AddAsync(issue);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Issue {IssueId} reported by user {UserId}", issue.IssueId, userId);

        return CreatedAtAction(nameof(GetIssueById), new { id = issue.IssueId },
            ApiResponse<IssueResponse>.SuccessResponse(MapToIssueResponse(issue)));
    }

    [HttpPost("report-with-photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReportIssueWithPhoto([FromForm] ReportIssueWithPhotoRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        //
        _logger.LogInformation("Received issue report - Type: '{Type}', Description: '{Description}', Latitude: {Lat}, Longitude: {Lng}",
            request.Type ?? "NULL", 
            request.Description ?? "NULL",
            request.Latitude,
            request.Longitude);

        // Upload photos if provided (support both old single photo and new 3-photo system)
        var photoUrls = new List<string>();

        // Check for new 3-photo system first
        var photo1 = request.Photo1 ?? request.Photo;  // Fallback to legacy Photo field
        if (photo1 != null)
        {
            if (!_fileStorageService.ValidateImage(photo1))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid image file for photo 1"));
            photoUrls.Add(await _fileStorageService.UploadImageAsync(photo1, "issues"));
        }

        if (request.Photo2 != null)
        {
            if (!_fileStorageService.ValidateImage(request.Photo2))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid image file for photo 2"));
            photoUrls.Add(await _fileStorageService.UploadImageAsync(request.Photo2, "issues"));
        }

        if (request.Photo3 != null)
        {
            if (!_fileStorageService.ValidateImage(request.Photo3))
                return BadRequest(ApiResponse<object>.ErrorResponse("Invalid image file for photo 3"));
            photoUrls.Add(await _fileStorageService.UploadImageAsync(request.Photo3, "issues"));
        }

        // Combine photo URLs with semicolon separator for multiple photos
        string? photoUrl = photoUrls.Count > 0 ? string.Join(";", photoUrls) : null;

        //
        if (string.IsNullOrWhiteSpace(request.Type))
        {
            // Cleanup uploaded photos if validation fails
            foreach (var url in photoUrls)
            {
                try { await _fileStorageService.DeleteImageAsync(url); } catch { }
            }
            _logger.LogWarning("Issue type is null or empty");
            return BadRequest(ApiResponse<object>.ErrorResponse("Issue type is required"));
        }

        // Try parsing with ignoreCase=true for case-insensitive matching
        var typeString = request.Type.Trim();
        if (!Enum.TryParse<IssueType>(typeString, ignoreCase: true, out var issueType))
        {
            // Cleanup uploaded photos if validation fails
            foreach (var url in photoUrls)
            {
                try { await _fileStorageService.DeleteImageAsync(url); } catch { }
            }
            _logger.LogWarning("Invalid issue type received: {Type}. Valid types are: {ValidTypes}", 
                typeString, string.Join(", ", Enum.GetNames<IssueType>()));
            return BadRequest(ApiResponse<object>.ErrorResponse(
                $"Invalid issue type: '{typeString}'. Valid types are: {string.Join(", ", Enum.GetNames<IssueType>())}"));
        }

        // Default severity to Moderate if not provided
        IssueSeverity issueSeverity = IssueSeverity.Moderate;
        if (!string.IsNullOrEmpty(request.Severity))
        {
            var severityString = request.Severity.Trim();
            //
            if (!Enum.TryParse<IssueSeverity>(severityString, ignoreCase: true, out issueSeverity))
            {
                // Cleanup uploaded photos if validation fails
                foreach (var url in photoUrls)
                {
                    try { await _fileStorageService.DeleteImageAsync(url); } catch { }
                }
                _logger.LogWarning("Invalid severity received: {Severity}. Valid severities are: {ValidSeverities}",
                    severityString, string.Join(", ", Enum.GetNames<IssueSeverity>()));
                return BadRequest(ApiResponse<object>.ErrorResponse(
                    $"Invalid severity: '{severityString}'. Valid severities are: {string.Join(", ", Enum.GetNames<IssueSeverity>())}"));
            }
        }

        // Generate title from type and date if not provided
        var title = request.Title;
        if (string.IsNullOrWhiteSpace(title))
        {
            title = $"{issueType} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        }

        //
        Issue issue;
        
        //
        try
        {
            issue = new Issue
            {
                Title = title,
                Description = request.Description ?? string.Empty,  // Handle null description
                Type = issueType,
                Severity = issueSeverity,
                ReportedByUserId = userId.Value,
                Latitude = request.Latitude ?? 0.0,
                Longitude = request.Longitude ?? 0.0,
                LocationDescription = request.LocationDescription,
                PhotoUrl = photoUrl,
                Status = IssueStatus.Reported,
                ReportedAt = DateTime.UtcNow,
                EventTime = DateTime.UtcNow,
                SyncTime = DateTime.UtcNow,
                IsSynced = true,
                SyncVersion = 1
            };

            await _issueRepo.AddAsync(issue);
            await _context.SaveChangesAsync();
        }
        catch
        {
            // Cleanup uploaded files if database save failed
            foreach (var url in photoUrls)
            {
                try
                {
                    await _fileStorageService.DeleteImageAsync(url);
                    _logger.LogWarning("Cleaned up orphaned photo file: {PhotoUrl}", url);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to cleanup orphaned photo: {PhotoUrl}", url);
                }
            }
            throw; // Re-throw original exception
        }

        _logger.LogInformation("Issue {IssueId} reported by user {UserId} with photo {PhotoUrl}",
            issue.IssueId, userId, photoUrl);

        return CreatedAtAction(nameof(GetIssueById), new { id = issue.IssueId },
            ApiResponse<IssueResponse>.SuccessResponse(MapToIssueResponse(issue)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIssueById(int id)
    {
        var issue = await _issueRepo.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Issue not found"));

        return Ok(ApiResponse<IssueResponse>.SuccessResponse(MapToIssueResponse(issue)));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllIssues([FromQuery] IssueStatus? status = null)
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        IEnumerable<Issue> issues;

        if (userRole == "Worker")
        {
            issues = await _issueRepo.GetUserIssuesAsync(userId!.Value);
        }
        else
        {
            issues = await _issueRepo.GetAllAsync();
        }

        if (status.HasValue)
        {
            issues = issues.Where(i => i.Status == status.Value);
        }

        return Ok(ApiResponse<IEnumerable<IssueResponse>>.SuccessResponse(
            issues.Select(i => MapToIssueResponse(i))));
    }

    [HttpGet("critical")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetCriticalIssues()
    {
        var issues = await _issueRepo.GetCriticalIssuesAsync();
        return Ok(ApiResponse<IEnumerable<IssueResponse>>.SuccessResponse(
            issues.Select(i => MapToIssueResponse(i))));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> UpdateIssueStatus(int id, [FromBody] UpdateIssueStatusRequest request)
    {
        var issue = await _issueRepo.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Issue not found"));

        var userId = GetCurrentUserId();

        issue.Status = request.Status;
        issue.SyncTime = DateTime.UtcNow;
        issue.SyncVersion++;

        if (request.Status == IssueStatus.Resolved)
        {
            issue.ResolvedAt = DateTime.UtcNow;
            issue.ResolvedByUserId = userId;
            issue.ResolutionNotes = request.ResolutionNotes;
        }

        await _issueRepo.UpdateAsync(issue);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Issue {IssueId} status updated to {Status} by user {UserId}",
            id, request.Status, userId);

        return Ok(ApiResponse<IssueResponse>.SuccessResponse(MapToIssueResponse(issue)));
    }

    [HttpGet("unresolved-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUnresolvedCount()
    {
        var issues = await _issueRepo.GetAllAsync();
        var unresolvedCount = issues.Count(i => i.Status != IssueStatus.Resolved);

        return Ok(ApiResponse<int>.SuccessResponse(unresolvedCount));
    }

    private IssueResponse MapToIssueResponse(Issue issue)
    {
        return new IssueResponse
        {
            IssueId = issue.IssueId,
            Title = issue.Title,
            Description = issue.Description,
            Type = issue.Type,
            Severity = issue.Severity,
            Status = issue.Status,
            ReportedByUserId = issue.ReportedByUserId,
            ZoneId = issue.ZoneId,
            Latitude = issue.Latitude,
            Longitude = issue.Longitude,
            LocationDescription = issue.LocationDescription,
            PhotoUrl = issue.PhotoUrl,
            ReportedAt = issue.ReportedAt,
            ResolvedAt = issue.ResolvedAt,
            ResolutionNotes = issue.ResolutionNotes
        };
    }
}
