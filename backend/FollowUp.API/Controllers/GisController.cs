using FollowUp.Core.DTOs.Gis;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;
using System.Text.Json;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
public class GisController : BaseApiController
{
    private readonly IGisService _gisService;
    private readonly IGisFileRepository _gisFiles;
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly ILogger<GisController> _logger;
    private readonly string _storagePath;

    private const long MaxFileSize = 10 * 1024 * 1024; // 10MB

    public GisController(
        IGisService gisService,
        IGisFileRepository gisFiles,
        IUserRepository users,
        IZoneRepository zones,
        ILogger<GisController> logger)
    {
        _gisService = gisService;
        _gisFiles = gisFiles;
        _users = users;
        _zones = zones;
        _logger = logger;
        _storagePath = Path.Combine(Directory.GetCurrentDirectory(), "Storage", "GIS");

        // Ensure storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }
    }

    /// <summary>
    /// Get current status of all GIS files
    /// </summary>
    [HttpGet("status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetGisFilesStatus()
    {
        var quarters = await _gisFiles.GetActiveByTypeAsync(GisFileType.Quarters);
        var borders = await _gisFiles.GetActiveByTypeAsync(GisFileType.Borders);
        var blocks = await _gisFiles.GetActiveByTypeAsync(GisFileType.Blocks);

        return Ok(new
        {
            success = true,
            data = new GisFilesStatusDto
            {
                Quarters = quarters != null ? MapToDto(quarters) : null,
                Borders = borders != null ? MapToDto(borders) : null,
                Blocks = blocks != null ? MapToDto(blocks) : null
            }
        });
    }

    /// <summary>
    /// Get all uploaded GIS files (history)
    /// </summary>
    [HttpGet("files")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllGisFiles()
    {
        var files = await _gisFiles.GetAllAsync();

        return Ok(new
        {
            success = true,
            data = files.Select(MapToDto)
        });
    }

    /// <summary>
    /// Upload a new GIS file (replaces existing file of same type)
    /// </summary>
    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [RequestSizeLimit(MaxFileSize)]
    public async Task<IActionResult> UploadGisFile(
        IFormFile file,
        [FromForm] GisFileType fileType,
        [FromForm] string? notes = null,
        [FromForm] bool autoImport = true)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        // Validate file
        if (file == null || file.Length == 0)
            return BadRequest(new { success = false, message = "لم يتم رفع أي ملف" });

        if (file.Length > MaxFileSize)
            return BadRequest(new { success = false, message = "حجم الملف يتجاوز الحد المسموح (10MB)" });

        var ext = Path.GetExtension(file.FileName).ToLower();
        if (ext != ".json" && ext != ".geojson")
            return BadRequest(new { success = false, message = "نوع الملف غير مدعوم. يرجى رفع ملف GeoJSON (.json أو .geojson)" });

        try
        {
            // Read and validate GeoJSON
            string jsonContent;
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                jsonContent = await reader.ReadToEndAsync();
            }

            // Basic validation - check if it's valid JSON with features
            int featuresCount = 0;
            try
            {
                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("features", out var features) && features.ValueKind == JsonValueKind.Array)
                {
                    featuresCount = features.GetArrayLength();
                }
                else if (root.TryGetProperty("type", out var type) && type.GetString() == "Feature")
                {
                    featuresCount = 1;
                }

                if (featuresCount == 0)
                {
                    return BadRequest(new { success = false, message = "ملف GeoJSON لا يحتوي على أي features" });
                }
            }
            catch (JsonException)
            {
                return BadRequest(new { success = false, message = "ملف JSON غير صالح" });
            }

            // Deactivate old files of same type
            await _gisFiles.DeactivateByTypeAsync(fileType);

            // Generate stored filename
            var storedFileName = $"{fileType.ToString().ToLower()}.geojson";
            var storedFilePath = Path.Combine(_storagePath, storedFileName);

            // Delete old file if exists
            if (System.IO.File.Exists(storedFilePath))
            {
                System.IO.File.Delete(storedFilePath);
            }

            // Save new file
            await System.IO.File.WriteAllTextAsync(storedFilePath, jsonContent);

            // Create database record
            var gisFile = new GisFile
            {
                FileType = fileType,
                OriginalFileName = file.FileName,
                StoredFileName = storedFileName,
                FileSize = file.Length,
                IsActive = true,
                FeaturesCount = featuresCount,
                UploadedByUserId = userId.Value,
                UploadedAt = DateTime.UtcNow,
                Notes = notes
            };

            await _gisFiles.AddAsync(gisFile);

            _logger.LogInformation("GIS file uploaded: {Type} by user {UserId}, {Features} features",
                fileType, userId, featuresCount);

            // Auto-import to zones if requested
            if (autoImport)
            {
                try
                {
                    if (fileType == GisFileType.Blocks)
                    {
                        await _gisService.ImportBlocksFromGeoJsonAsync(storedFilePath, user.MunicipalityId);
                    }
                    else
                    {
                        await _gisService.ImportGeoJsonAsync(storedFilePath, user.MunicipalityId);
                    }

                    gisFile.LastImportedAt = DateTime.UtcNow;
                    await _gisFiles.UpdateAsync(gisFile);

                    _logger.LogInformation("GIS file auto-imported to zones: {Type}", fileType);
                }
                catch (Exception importEx)
                {
                    _logger.LogWarning(importEx, "Failed to auto-import GIS file to zones");
                    // Don't fail the upload, just warn
                    return Ok(new
                    {
                        success = true,
                        message = "تم رفع الملف بنجاح، لكن فشل الاستيراد التلقائي. يمكنك الاستيراد يدوياً لاحقاً.",
                        warning = importEx.Message,
                        data = MapToDto(gisFile)
                    });
                }
            }

            return Ok(new
            {
                success = true,
                message = autoImport
                    ? $"تم رفع واستيراد الملف بنجاح ({featuresCount} منطقة)"
                    : $"تم رفع الملف بنجاح ({featuresCount} منطقة). يمكنك استيراده لاحقاً.",
                data = MapToDto(gisFile)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload GIS file");
            return BadRequest(new { success = false, message = $"فشل رفع الملف: {ex.Message}" });
        }
    }

    /// <summary>
    /// Import a previously uploaded GIS file to zones
    /// </summary>
    [HttpPost("import/{fileId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportGisFile(int fileId)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        var gisFile = await _gisFiles.GetByIdAsync(fileId);
        if (gisFile == null)
            return NotFound(new { success = false, message = "الملف غير موجود" });

        var filePath = Path.Combine(_storagePath, gisFile.StoredFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { success = false, message = "الملف غير موجود على السيرفر" });

        try
        {
            if (gisFile.FileType == GisFileType.Blocks)
            {
                await _gisService.ImportBlocksFromGeoJsonAsync(filePath, user.MunicipalityId);
            }
            else
            {
                await _gisService.ImportGeoJsonAsync(filePath, user.MunicipalityId);
            }

            gisFile.LastImportedAt = DateTime.UtcNow;
            await _gisFiles.UpdateAsync(gisFile);

            return Ok(new
            {
                success = true,
                message = $"تم استيراد {gisFile.FeaturesCount} منطقة بنجاح"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import GIS file {FileId}", fileId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a GIS file
    /// </summary>
    [HttpDelete("files/{fileId}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteGisFile(int fileId)
    {
        var gisFile = await _gisFiles.GetByIdAsync(fileId);
        if (gisFile == null)
            return NotFound(new { success = false, message = "الملف غير موجود" });

        try
        {
            // Delete physical file if it exists
            var filePath = Path.Combine(_storagePath, gisFile.StoredFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            await _gisFiles.DeleteAsync(fileId);

            return Ok(new { success = true, message = "تم حذف الملف بنجاح" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete GIS file {FileId}", fileId);
            return BadRequest(new { success = false, message = ex.Message });
        }
    }

    /// <summary>
    /// Download a GIS file
    /// </summary>
    [HttpGet("files/{fileId}/download")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DownloadGisFile(int fileId)
    {
        var gisFile = await _gisFiles.GetByIdAsync(fileId);
        if (gisFile == null)
            return NotFound(new { success = false, message = "الملف غير موجود" });

        var filePath = Path.Combine(_storagePath, gisFile.StoredFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(new { success = false, message = "الملف غير موجود على السيرفر" });

        var content = await System.IO.File.ReadAllBytesAsync(filePath);
        return File(content, "application/geo+json", gisFile.OriginalFileName);
    }

    /// <summary>
    /// Get zones count by type
    /// </summary>
    [HttpGet("zones-summary")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetZonesSummary()
    {
        var zones = await _zones.GetActiveZonesAsync();
        var zonesList = zones.ToList();

        return Ok(new
        {
            success = true,
            data = new
            {
                totalZones = zonesList.Count,
                byType = zonesList.GroupBy(z => z.District ?? "غير محدد")
                    .Select(g => new { type = g.Key, count = g.Count() })
            }
        });
    }

    // Legacy endpoints for backward compatibility

    [HttpPost("import-blocks")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ImportBlocks([FromBody] ImportBlocksRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        try
        {
            await _gisService.ImportBlocksFromGeoJsonAsync(
                request.GeoJsonFilePath,
                user.MunicipalityId
            );

            return Ok(new
            {
                success = true,
                message = "Blocks imported successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import blocks");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    [HttpPost("import-geojson")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ImportGeoJson([FromBody] GisImportGeoJsonRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        try
        {
            await _gisService.ImportGeoJsonAsync(
                request.GeoJsonFilePath,
                user.MunicipalityId
            );

            return Ok(new
            {
                success = true,
                message = "GeoJSON imported successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import GeoJSON");
            return BadRequest(new
            {
                success = false,
                message = ex.Message
            });
        }
    }

    private static GisFileDto MapToDto(GisFile gisFile)
    {
        return new GisFileDto
        {
            GisFileId = gisFile.GisFileId,
            FileType = gisFile.FileType.ToString(),
            OriginalFileName = gisFile.OriginalFileName,
            FileSize = gisFile.FileSize,
            IsActive = gisFile.IsActive,
            FeaturesCount = gisFile.FeaturesCount,
            UploadedByName = gisFile.UploadedBy?.FullName,
            UploadedAt = gisFile.UploadedAt,
            LastImportedAt = gisFile.LastImportedAt,
            Notes = gisFile.Notes
        };
    }
}

public class ImportBlocksRequest
{
    public string GeoJsonFilePath { get; set; } = string.Empty;
}

public class GisImportGeoJsonRequest
{
    public string GeoJsonFilePath { get; set; } = string.Empty;
}

public class GisImportShapefileRequest
{
    public string FilePath { get; set; } = string.Empty;
}
