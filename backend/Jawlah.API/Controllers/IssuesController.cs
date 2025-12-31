using System.Security.Claims;
using AutoMapper;
using Jawlah.API.Models;
using Jawlah.API.Utils;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Issues;
using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class IssuesController : BaseApiController
{
    private readonly IIssueRepository _issues;
    private readonly IPhotoRepository _photos;
    private readonly ILogger<IssuesController> _logger;
    private readonly IFileStorageService _files;
    private readonly IMapper _mapper;

    public IssuesController(
        IIssueRepository issues,
        IPhotoRepository photos,
        ILogger<IssuesController> logger,
        IFileStorageService files,
        IMapper mapper)
    {
        _issues = issues;
        _photos = photos;
        _logger = logger;
        _files = files;
        _mapper = mapper;
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

        await _issues.AddAsync(issue);
        await _issues.SaveChangesAsync();

        return CreatedAtAction(nameof(GetIssueById), new { id = issue.IssueId },
            ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    [HttpPost("report-with-photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReportIssueWithPhoto([FromForm] ReportIssueWithPhotoRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // support up to 3 photos per issue
        var photoUrls = new List<string>();

        var photo1 = request.Photo1 ?? request.Photo;
        if (photo1 != null)
        {
            if (!_files.ValidateImage(photo1))
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة الأولى غير صالح"));
            photoUrls.Add(await _files.UploadImageAsync(photo1, "issues"));
        }

        if (request.Photo2 != null)
        {
            if (!_files.ValidateImage(request.Photo2))
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة الثانية غير صالح"));
            photoUrls.Add(await _files.UploadImageAsync(request.Photo2, "issues"));
        }

        if (request.Photo3 != null)
        {
            if (!_files.ValidateImage(request.Photo3))
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة الثالثة غير صالح"));
            photoUrls.Add(await _files.UploadImageAsync(request.Photo3, "issues"));
        }

        string? photoUrl = photoUrls.Count > 0 ? string.Join(";", photoUrls) : null;

        if (string.IsNullOrWhiteSpace(request.Type))
        {
            await _files.DeleteImagesAsync(photoUrls);
            return BadRequest(ApiResponse<object>.ErrorResponse("نوع المشكلة مطلوب"));
        }

        var typeString = request.Type.Trim();
        if (!Enum.TryParse<IssueType>(typeString, ignoreCase: true, out var issueType))
        {
            await _files.DeleteImagesAsync(photoUrls);
            return BadRequest(ApiResponse<object>.ErrorResponse("نوع المشكلة غير صالح"));
        }

        IssueSeverity issueSeverity = IssueSeverity.Moderate;
        if (!string.IsNullOrEmpty(request.Severity))
        {
            var severityString = request.Severity.Trim();
            if (!Enum.TryParse<IssueSeverity>(severityString, ignoreCase: true, out issueSeverity))
            {
                await _files.DeleteImagesAsync(photoUrls);
                return BadRequest(ApiResponse<object>.ErrorResponse("مستوى الخطورة غير صالح"));
            }
        }

        var title = InputSanitizer.SanitizeString(request.Title, 200);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = $"{issueType} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        }

        var sanitizedDescription = InputSanitizer.SanitizeString(request.Description, 2000);
        var sanitizedLocation = InputSanitizer.SanitizeString(request.LocationDescription, 500);

        Issue issue;

        try
        {
            issue = new Issue
            {
                Title = title,
                Description = sanitizedDescription,
                Type = issueType,
                Severity = issueSeverity,
                ReportedByUserId = userId.Value,
                Latitude = request.Latitude ?? 0.0,
                Longitude = request.Longitude ?? 0.0,
                LocationDescription = sanitizedLocation,
                PhotoUrl = photoUrl, // keep for backward compatibility
                Status = IssueStatus.Reported,
                ReportedAt = DateTime.UtcNow,
                EventTime = DateTime.UtcNow,
                SyncTime = DateTime.UtcNow,
                IsSynced = true,
                SyncVersion = 1
            };

            await _issues.AddAsync(issue);
            await _issues.SaveChangesAsync();

            for (int i = 0; i < photoUrls.Count; i++)
            {
                var photo = new Photo
                {
                    PhotoUrl = photoUrls[i],
                    EntityType = "Issue",
                    EntityId = issue.IssueId,
                    OrderIndex = i,
                    UploadedAt = DateTime.UtcNow,
                    UploadedByUserId = userId.Value,
                    CreatedAt = DateTime.UtcNow
                };
                await _photos.AddAsync(photo);
            }
            await _issues.SaveChangesAsync();
        }
        catch
        {
            await _files.DeleteImagesAsync(photoUrls);
            throw;
        }

        return CreatedAtAction(nameof(GetIssueById), new { id = issue.IssueId },
            ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetIssueById(int id)
    {
        var issue = await _issues.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المشكلة غير موجودة"));

        return Ok(ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllIssues([FromQuery] IssueStatus? status = null)
    {
        var userId = GetCurrentUserId();
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

        IEnumerable<Issue> issues;

        if (userRole == "Worker")
        {
            issues = await _issues.GetUserIssuesAsync(userId!.Value);
        }
        else
        {
            issues = await _issues.GetAllAsync();
        }

        if (status.HasValue)
        {
            issues = issues.Where(i => i.Status == status.Value);
        }

        return Ok(ApiResponse<IEnumerable<IssueResponse>>.SuccessResponse(
            issues.Select(i => _mapper.Map<IssueResponse>(i))));
    }

    [HttpGet("critical")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetCriticalIssues()
    {
        var issues = await _issues.GetCriticalIssuesAsync();
        return Ok(ApiResponse<IEnumerable<IssueResponse>>.SuccessResponse(
            issues.Select(i => _mapper.Map<IssueResponse>(i))));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> UpdateIssueStatus(int id, [FromBody] UpdateIssueStatusRequest request)
    {
        var issue = await _issues.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المشكلة غير موجودة"));

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

        try
        {
            await _issues.UpdateAsync(issue);
            await _issues.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            _logger.LogWarning("Concurrency conflict when updating issue {IssueId}", id);
            return Conflict(ApiResponse<object>.ErrorResponse(
                "تم تعديل المشكلة من قبل مستخدم آخر. يرجى التحديث والمحاولة مرة أخرى"));
        }

        return Ok(ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    [HttpGet("unresolved-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUnresolvedCount()
    {
        var issues = await _issues.GetAllAsync();
        var unresolvedCount = issues.Count(i => i.Status != IssueStatus.Resolved);

        return Ok(ApiResponse<int>.SuccessResponse(unresolvedCount));
    }
}
