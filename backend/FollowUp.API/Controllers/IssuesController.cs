using AutoMapper;
using FollowUp.API.Models;
using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Issues;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace FollowUp.API.Controllers;

// this controller handle issue reporting and managment
[Route("api/[controller]")]
public class IssuesController : BaseApiController
{
    private readonly IIssueRepository _issues;
    private readonly IPhotoRepository _photos;
    private readonly IUserRepository _users;
    private readonly IMunicipalityRepository _municipalities;
    private readonly ILogger<IssuesController> _logger;
    private readonly IFileStorageService _files;
    private readonly INotificationService _notifications;
    private readonly IMapper _mapper;

    public IssuesController(
        IIssueRepository issues,
        IPhotoRepository photos,
        IUserRepository users,
        IMunicipalityRepository municipalities,
        ILogger<IssuesController> logger,
        IFileStorageService files,
        INotificationService notifications,
        IMapper mapper)
    {
        _issues = issues;
        _photos = photos;
        _users = users;
        _municipalities = municipalities;
        _logger = logger;
        _files = files;
        _notifications = notifications;
        _mapper = mapper;
    }

    // report new issue without photo
    [HttpPost("report")]
    public async Task<IActionResult> ReportIssue([FromBody] ReportIssueRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // Get user to retrieve their municipality
        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        // Validate GPS coordinates
        var validationResult = ValidateGpsCoordinates(request.Latitude, request.Longitude);
        if (validationResult != null)
            return BadRequest(ApiResponse<object>.ErrorResponse("إحداثيات GPS غير صالحة. يرجى التأكد من تفعيل الموقع"));

        // clean inputs to prevent xss
        var sanitizedTitle = InputSanitizer.SanitizeString(request.Title, 200);
        var sanitizedDescription = InputSanitizer.SanitizeString(request.Description, 2000);
        var sanitizedLocation = InputSanitizer.SanitizeString(request.LocationDescription, 500);

        // create issue entity
        var issue = new Issue
        {
            Title = sanitizedTitle,
            Description = sanitizedDescription,
            Type = request.Type,
            Severity = request.Severity,
            ReportedByUserId = userId.Value,
            MunicipalityId = user.MunicipalityId,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LocationDescription = sanitizedLocation,
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

        _logger.LogInformation("Issue {IssueId} reported by user {UserId}", issue.IssueId, userId.Value);

        // notify supervisors about the new issue
        try
        {
            var workerName = user.FullName ?? "عامل";
            await _notifications.SendIssueReportedToSupervisorsAsync(
                issue.IssueId,
                issue.Title,
                workerName,
                issue.Severity.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send issue reported notification for issue {IssueId}", issue.IssueId);
        }

        return CreatedAtAction(nameof(GetIssueById), new { id = issue.IssueId },
            ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    // report issue with photo upload
    [HttpPost("report-with-photo")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ReportIssueWithPhoto([FromForm] ReportIssueWithPhotoRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // Get user to retrieve their municipality
        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        // we support up to 3 photos per issue
        var photoUrls = new List<string>();

        // validate photos first before uploading
        var photo1 = request.Photo1 ?? request.Photo;
        if (photo1 != null && !_files.ValidateImage(photo1))
            return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة الأولى غير صالح"));

        if (request.Photo2 != null && !_files.ValidateImage(request.Photo2))
            return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة الثانية غير صالح"));

        if (request.Photo3 != null && !_files.ValidateImage(request.Photo3))
            return BadRequest(ApiResponse<object>.ErrorResponse("ملف الصورة الثالثة غير صالح"));

        // now upload all photos
        if (photo1 != null)
            photoUrls.Add(await _files.UploadImageAsync(photo1, "issues"));

        if (request.Photo2 != null)
            photoUrls.Add(await _files.UploadImageAsync(request.Photo2, "issues"));

        if (request.Photo3 != null)
            photoUrls.Add(await _files.UploadImageAsync(request.Photo3, "issues"));

        string? photoUrl = photoUrls.Count > 0 ? string.Join(";", photoUrls) : null;

        // check issue type is provided
        if (string.IsNullOrWhiteSpace(request.Type))
        {
            await _files.DeleteImagesAsync(photoUrls);
            return BadRequest(ApiResponse<object>.ErrorResponse("نوع المشكلة مطلوب"));
        }

        // parse issue type
        var typeString = request.Type.Trim();
        if (!Enum.TryParse<IssueType>(typeString, ignoreCase: true, out var issueType))
        {
            await _files.DeleteImagesAsync(photoUrls);
            return BadRequest(ApiResponse<object>.ErrorResponse("نوع المشكلة غير صالح"));
        }

        // parse severity with default
        IssueSeverity issueSeverity = IssueSeverity.Medium;
        if (!string.IsNullOrEmpty(request.Severity))
        {
            var severityString = request.Severity.Trim();
            if (!Enum.TryParse<IssueSeverity>(severityString, ignoreCase: true, out issueSeverity))
            {
                await _files.DeleteImagesAsync(photoUrls);
                return BadRequest(ApiResponse<object>.ErrorResponse("مستوى الخطورة غير صالح"));
            }
        }

        // clean and default title
        var title = InputSanitizer.SanitizeString(request.Title, 200);
        if (string.IsNullOrWhiteSpace(title))
        {
            title = $"{issueType} - {DateTime.UtcNow:yyyy-MM-dd HH:mm}";
        }

        var sanitizedDescription = InputSanitizer.SanitizeString(request.Description, 2000);
        var sanitizedLocation = InputSanitizer.SanitizeString(request.LocationDescription, 500);

        // check gps coords are provided
        if (!request.Latitude.HasValue || !request.Longitude.HasValue ||
            (request.Latitude.Value == 0 && request.Longitude.Value == 0))
        {
            await _files.DeleteImagesAsync(photoUrls);
            return BadRequest(ApiResponse<object>.ErrorResponse("يجب توفير إحداثيات الموقع"));
        }

        // check coords are inside work area
        if (request.Latitude.Value < Core.Constants.GeofencingConstants.MinLatitude ||
            request.Latitude.Value > Core.Constants.GeofencingConstants.MaxLatitude ||
            request.Longitude.Value < Core.Constants.GeofencingConstants.MinLongitude ||
            request.Longitude.Value > Core.Constants.GeofencingConstants.MaxLongitude)
        {
            await _files.DeleteImagesAsync(photoUrls);
            return BadRequest(ApiResponse<object>.ErrorResponse("الموقع خارج منطقة العمل المسموح بها"));
        }

        Issue issue;

        try
        {
            // create issue entity
            issue = new Issue
            {
                Title = title,
                Description = sanitizedDescription,
                Type = issueType,
                Severity = issueSeverity,
                ReportedByUserId = userId.Value,
                MunicipalityId = user.MunicipalityId,
                Latitude = request.Latitude.Value,
                Longitude = request.Longitude.Value,
                LocationDescription = sanitizedLocation,
                PhotoUrl = photoUrl,
                Status = IssueStatus.Reported,
                ReportedAt = DateTime.UtcNow,
                EventTime = DateTime.UtcNow,
                SyncTime = DateTime.UtcNow,
                IsSynced = true,
                SyncVersion = 1
            };

            await _issues.AddAsync(issue);
            await _issues.SaveChangesAsync();

            // save photos to photos table
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

                // add to issue's Photos collection so mapper includes it in response
                issue.Photos.Add(photo);
            }
            await _issues.SaveChangesAsync();

            _logger.LogInformation("Issue {IssueId} reported with {PhotoCount} photos by user {UserId}",
                issue.IssueId, photoUrls.Count, userId.Value);
        }
        catch
        {
            // delete uploaded photos if save failed
            await _files.DeleteImagesAsync(photoUrls);
            throw;
        }

        // notify supervisors about the new issue
        try
        {
            var workerName = user.FullName ?? "عامل";
            await _notifications.SendIssueReportedToSupervisorsAsync(
                issue.IssueId,
                issue.Title,
                workerName,
                issue.Severity.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send issue reported notification for issue {IssueId}", issue.IssueId);
        }

        return CreatedAtAction(nameof(GetIssueById), new { id = issue.IssueId },
            ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    // get single issue by id
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetIssueById(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var issue = await _issues.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المشكلة غير موجودة"));

        // Workers can only see their own issues
        var userRole = GetCurrentUserRole();
        if (userRole == "Worker" && issue.ReportedByUserId != userId.Value)
            return Forbid();

        return Ok(ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    // get all issues with filtering
    [HttpGet]
    public async Task<IActionResult> GetAllIssues(
        [FromQuery] IssueStatus? status = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized(ApiResponse<IEnumerable<IssueResponse>>.ErrorResponse("المستخدم غير مسجل الدخول"));

        // fix pagination values
        if (pageSize < 1 || pageSize > 100) pageSize = 50;
        if (page < 1) page = 1;

        var userRole = GetCurrentUserRole();

        IEnumerable<Issue> issues;

        // workers see only there own issues
        if (userRole == "Worker")
        {
            issues = await _issues.GetUserIssuesAsync(userId.Value);
        }
        else
        {
            issues = await _issues.GetAllAsync();
        }

        // filter by status if provided
        if (status.HasValue)
        {
            issues = issues.Where(i => i.Status == status.Value);
        }

        // paginate results
        var totalCount = issues.Count();
        var pagedIssues = issues
            .OrderByDescending(i => i.ReportedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize);

        // return issues as array directly (mobile and web expect this format)
        var issueResponses = pagedIssues.Select(i => _mapper.Map<IssueResponse>(i)).ToList();
        return Ok(ApiResponse<IEnumerable<IssueResponse>>.SuccessResponse(issueResponses));
    }

    // get critical issues for supervisors
    [HttpGet("critical")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetCriticalIssues()
    {
        var issues = await _issues.GetCriticalIssuesAsync();
        return Ok(ApiResponse<IEnumerable<IssueResponse>>.SuccessResponse(
            issues.Select(i => _mapper.Map<IssueResponse>(i))));
    }

    // update issue status
    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> UpdateIssueStatus(int id, [FromBody] UpdateIssueStatusRequest request)
    {
        var issue = await _issues.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المشكلة غير موجودة"));

        var userId = GetCurrentUserId();

        // update status
        issue.Status = request.Status;
        issue.SyncTime = DateTime.UtcNow;
        issue.SyncVersion++;

        // set resolution data if resolved
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

            _logger.LogInformation("Issue {IssueId} status updated to {Status} by user {UserId}",
                id, request.Status, userId);

            // notify the worker who reported this issue about the status change
            try
            {
                await _notifications.SendIssueReviewedNotificationAsync(
                    issue.ReportedByUserId,
                    issue.IssueId,
                    request.Status.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send issue status notification for issue {IssueId}", issue.IssueId);
            }
        }
        catch (DbUpdateConcurrencyException)
        {
            // someone else edited this issue
            _logger.LogWarning("Concurrency conflict when updating issue {IssueId}", id);
            return Conflict(ApiResponse<object>.ErrorResponse(
                "تم تعديل المشكلة من قبل مستخدم آخر. يرجى التحديث والمحاولة مرة أخرى"));
        }

        return Ok(ApiResponse<IssueResponse>.SuccessResponse(_mapper.Map<IssueResponse>(issue)));
    }

    // get count of unresolved issues
    [HttpGet("unresolved-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUnresolvedCount()
    {
        var issues = await _issues.GetAllAsync();
        var unresolvedCount = issues.Count(i => i.Status != IssueStatus.Resolved && i.Status != IssueStatus.Dismissed);

        return Ok(ApiResponse<int>.SuccessResponse(unresolvedCount));
    }

    // delete issue
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> DeleteIssue(int id)
    {
        var issue = await _issues.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("البلاغ غير موجود"));

        // get photos from photos table
        var photos = await _photos.GetPhotosByEntityAsync("Issue", id);
        var photoUrls = photos.Select(p => p.PhotoUrl).Where(u => !string.IsNullOrEmpty(u)).ToList();

        // also get photos from legacy field
        if (!string.IsNullOrEmpty(issue.PhotoUrl))
        {
            var legacyUrls = issue.PhotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
            photoUrls.AddRange(legacyUrls);
        }

        // delete photo files from disk
        if (photoUrls.Any())
        {
            await _files.DeleteImagesAsync(photoUrls!);
        }

        // delete photo records from db
        foreach (var photo in photos)
        {
            await _photos.DeleteAsync(photo);
        }

        await _issues.DeleteAsync(issue);
        await _issues.SaveChangesAsync();

        _logger.LogInformation("Issue {IssueId} deleted with {PhotoCount} photos", id, photoUrls.Count);
        return NoContent();
    }

    // ============ PDF Generation ============

    /// <summary>
    /// Generate and download PDF report for an issue
    /// </summary>
    [HttpGet("{id}/pdf")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> DownloadIssuePdf(int id)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var issue = await _issues.GetByIdAsync(id);
        if (issue == null)
            return NotFound(ApiResponse<object>.ErrorResponse("البلاغ غير موجود"));

        // Get photos
        var photos = await _photos.GetPhotosByEntityAsync("Issue", id);
        var photoUrls = photos.Select(p => p.PhotoUrl).ToList();

        // Also check legacy PhotoUrl field
        if (!string.IsNullOrEmpty(issue.PhotoUrl))
        {
            var legacyUrls = issue.PhotoUrl.Split(';', StringSplitOptions.RemoveEmptyEntries);
            photoUrls.AddRange(legacyUrls);
        }

        // Get reporter name and municipality
        var reporter = await _users.GetByIdAsync(issue.ReportedByUserId);
        var reporterName = reporter?.FullName ?? "غير معروف";

        // Get municipality name for PDF header
        var municipality = reporter != null
            ? await _municipalities.GetByIdAsync(reporter.MunicipalityId)
            : null;
        var municipalityName = municipality?.Name ?? "FollowUp";

        // Configure QuestPDF license (Community license for open source)
        QuestPDF.Settings.License = LicenseType.Community;

        // Arabic text mappings
        var severityMap = new Dictionary<IssueSeverity, string>
        {
            { IssueSeverity.Minor, "بسيطة" },
            { IssueSeverity.Medium, "متوسطة" },
            { IssueSeverity.Major, "كبيرة" },
            { IssueSeverity.Critical, "حرجة" }
        };

        var statusMap = new Dictionary<IssueStatus, string>
        {
            { IssueStatus.Reported, "تم الإبلاغ" },
            { IssueStatus.UnderReview, "قيد المراجعة" },
            { IssueStatus.Resolved, "تم الحل" },
            { IssueStatus.Dismissed, "تم الرفض" }
        };

        var typeMap = new Dictionary<IssueType, string>
        {
            { IssueType.Infrastructure, "بنية تحتية" },
            { IssueType.Safety, "سلامة" },
            { IssueType.Sanitation, "نظافة" },
            { IssueType.Equipment, "معدات" },
            { IssueType.Other, "أخرى" }
        };

        // Google Maps link
        var mapsLink = $"https://www.google.com/maps?q={issue.Latitude},{issue.Longitude}";

        // Generate PDF
        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(12));

                // Header
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text(municipalityName).Bold().FontSize(20);
                    col.Item().AlignCenter().Text("تقرير بلاغ ميداني").FontSize(16);
                    col.Item().PaddingVertical(10).LineHorizontal(1);
                });

                // Content
                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // Issue ID and Date
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Text($"رقم البلاغ: {issue.IssueId}").Bold();
                        row.RelativeItem().AlignLeft().Text($"التاريخ: {issue.ReportedAt:yyyy-MM-dd HH:mm}");
                    });

                    // Title
                    col.Item().Background(Colors.Grey.Lighten3).Padding(10).Column(inner =>
                    {
                        inner.Item().Text("عنوان البلاغ:").Bold();
                        inner.Item().Text(issue.Title);
                    });

                    // Description
                    col.Item().Column(inner =>
                    {
                        inner.Item().Text("الوصف:").Bold();
                        inner.Item().Text(issue.Description ?? "لا يوجد وصف");
                    });

                    // Details Grid
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn();
                            columns.RelativeColumn();
                        });

                        table.Cell().Border(1).Padding(5).Text("النوع:").Bold();
                        table.Cell().Border(1).Padding(5).Text(typeMap.GetValueOrDefault(issue.Type, issue.Type.ToString()));

                        table.Cell().Border(1).Padding(5).Text("الأولوية:").Bold();
                        table.Cell().Border(1).Padding(5).Text(severityMap.GetValueOrDefault(issue.Severity, issue.Severity.ToString()));

                        table.Cell().Border(1).Padding(5).Text("الحالة:").Bold();
                        table.Cell().Border(1).Padding(5).Text(statusMap.GetValueOrDefault(issue.Status, issue.Status.ToString()));

                        table.Cell().Border(1).Padding(5).Text("المبلّغ:").Bold();
                        table.Cell().Border(1).Padding(5).Text(reporterName);

                        table.Cell().Border(1).Padding(5).Text("المنطقة:").Bold();
                        table.Cell().Border(1).Padding(5).Text(issue.Zone?.ZoneName ?? "غير محدد");

                        table.Cell().Border(1).Padding(5).Text("وصف الموقع:").Bold();
                        table.Cell().Border(1).Padding(5).Text(issue.LocationDescription ?? "غير محدد");
                    });

                    // Location with Google Maps link
                    col.Item().Background(Colors.Blue.Lighten4).Padding(10).Column(inner =>
                    {
                        inner.Item().Text("الموقع:").Bold();
                        inner.Item().Text($"الإحداثيات: {issue.Latitude:F6}, {issue.Longitude:F6}");
                        inner.Item().Text($"رابط خرائط جوجل: {mapsLink}").FontColor(Colors.Blue.Medium);
                    });

                    // Photos section
                    if (photoUrls.Any())
                    {
                        col.Item().PaddingTop(10).Text($"الصور المرفقة ({photoUrls.Count}):").Bold();

                        foreach (var photoUrl in photoUrls.Take(3))
                        {
                            try
                            {
                                // SECURITY: Validate photo path to prevent path traversal attacks
                                var fullPath = GetSafePhotoPath(photoUrl);
                                if (fullPath != null && System.IO.File.Exists(fullPath))
                                {
                                    var imageData = System.IO.File.ReadAllBytes(fullPath);
                                    col.Item().AlignCenter().Width(300).Image(imageData);
                                }
                                else
                                {
                                    col.Item().Text($"صورة: {Path.GetFileName(photoUrl)}").FontColor(Colors.Grey.Medium);
                                }
                            }
                            catch
                            {
                                col.Item().Text($"صورة: {Path.GetFileName(photoUrl)}").FontColor(Colors.Grey.Medium);
                            }
                        }
                    }
                    else
                    {
                        col.Item().Text("لا توجد صور مرفقة").FontColor(Colors.Grey.Medium);
                    }
                });

                // Footer
                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("تم إنشاء هذا التقرير بواسطة نظام جولة - ");
                    text.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm"));
                });
            });
        });

        // Generate PDF bytes
        var pdfBytes = document.GeneratePdf();

        _logger.LogInformation("Issue {IssueId} PDF downloaded by user {UserId}", id, userId.Value);

        // Return PDF file
        var fileName = $"issue_report_{issue.IssueId}_{DateTime.Now:yyyyMMdd}.pdf";
        return File(pdfBytes, "application/pdf", fileName);
    }

    /// <summary>
    /// Validates and returns a safe file path for photos, preventing path traversal attacks.
    /// Returns null if the path is invalid or outside the allowed storage directory.
    /// </summary>
    private string? GetSafePhotoPath(string photoUrl)
    {
        if (string.IsNullOrEmpty(photoUrl))
            return null;

        // Block obvious path traversal attempts
        if (photoUrl.Contains("..") || photoUrl.Contains("~"))
            return null;

        string relativePath;

        // Handle full URLs or relative paths
        if (photoUrl.StartsWith("http://") || photoUrl.StartsWith("https://"))
        {
            try
            {
                var uri = new Uri(photoUrl);
                var pathParts = uri.AbsolutePath.TrimStart('/').Split('/');

                // Extract relative path based on URL format
                if (pathParts.Length >= 3 && pathParts[0] == "api" && pathParts[1] == "files")
                {
                    relativePath = string.Join("/", pathParts.Skip(2));
                }
                else if (pathParts.Length >= 2 && pathParts[0] == "uploads")
                {
                    relativePath = string.Join("/", pathParts.Skip(1));
                }
                else
                {
                    return null; // Unknown URL format
                }
            }
            catch
            {
                return null;
            }
        }
        else
        {
            // Remove leading slashes and normalize
            relativePath = photoUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        }

        // Build the full path
        var storageRoot = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "uploads");
        var fullPath = Path.GetFullPath(Path.Combine(storageRoot, relativePath));

        // SECURITY: Ensure the resolved path is still within our storage directory
        if (!fullPath.StartsWith(Path.GetFullPath(storageRoot), StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Blocked path traversal attempt: {PhotoUrl}", photoUrl);
            return null;
        }

        return fullPath;
    }
}
