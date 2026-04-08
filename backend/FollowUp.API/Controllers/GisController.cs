using FollowUp.Core.Constants;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Gis;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Swashbuckle.AspNetCore.Annotations;
using System.IO.Compression;
using System.Text.Json;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("GIS")]
public class GisController : BaseApiController
{
    private readonly IGisService _gisService;
    private readonly IGisFileRepository _gisFiles;
    private readonly IUserRepository _users;
    private readonly IZoneRepository _zones;
    private readonly ILogger<GisController> _logger;
    private readonly string _storagePath;

    private const long MaxFileSize = AppConstants.MaxGisFileSizeBytes;

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

    [HttpGet("status")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "get status of all gis files")]
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

    [HttpGet("files")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "get all uploaded gis files")]
    public async Task<IActionResult> GetAllGisFiles()
    {
        var files = await _gisFiles.GetAllAsync();

        return Ok(new
        {
            success = true,
            data = files.Select(MapToDto)
        });
    }

    [HttpPost("upload")]
    [Authorize(Roles = "Admin")]
    [EnableRateLimiting("upload")]
    [RequestSizeLimit(MaxFileSize)]
    [SwaggerOperation(Summary = "upload a new gis file")]
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
            return BadRequest(ApiResponse<object>.ErrorResponse("لم يتم رفع أي ملف"));

        if (file.Length > MaxFileSize)
            return BadRequest(ApiResponse<object>.ErrorResponse("حجم الملف يتجاوز الحد المسموح (10MB)"));

        var ext = Path.GetExtension(file.FileName).ToLower();
        var supportedExtensions = new[] { ".json", ".geojson", ".shp", ".zip" };
        if (!supportedExtensions.Contains(ext))
            return BadRequest(ApiResponse<object>.ErrorResponse("نوع الملف غير مدعوم. يرجى رفع ملف GeoJSON (.json/.geojson) أو Shapefile (.shp/.zip)"));

        try
        {
            bool isShapefile = ext == ".shp" || ext == ".zip";
            int featuresCount = 0;
            string storedFileName;
            string storedFilePath;

            if (isShapefile)
            {
                // Handle Shapefile upload (.shp or .zip containing shapefile)
                string shpBasePath;

                if (ext == ".zip")
                {
                    // Extract ZIP to system temp directory (avoids polluting storage)
                    var extractDir = Path.Combine(Path.GetTempPath(), $"gis_extract_{Guid.NewGuid():N}");
                    Directory.CreateDirectory(extractDir);

                    try
                    {
                        using (var stream = file.OpenReadStream())
                        using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        {
                            // Extract with ZIP slip protection
                            foreach (var entry in archive.Entries)
                            {
                                var destinationPath = Path.GetFullPath(Path.Combine(extractDir, entry.FullName));
                                if (!destinationPath.StartsWith(Path.GetFullPath(extractDir) + Path.DirectorySeparatorChar)
                                    && destinationPath != Path.GetFullPath(extractDir))
                                {
                                    throw new InvalidDataException("ZIP entry has a path that escapes the target directory");
                                }

                                if (string.IsNullOrEmpty(entry.Name))
                                {
                                    // Entry is a directory
                                    Directory.CreateDirectory(destinationPath);
                                }
                                else
                                {
                                    Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
                                    entry.ExtractToFile(destinationPath, overwrite: true);
                                }
                            }
                        }

                        // Find .shp file inside extracted directory
                        var shpFiles = Directory.GetFiles(extractDir, "*.shp", SearchOption.AllDirectories);
                        if (shpFiles.Length == 0)
                        {
                            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                            return BadRequest(ApiResponse<object>.ErrorResponse("ملف ZIP لا يحتوي على ملف Shapefile (.shp)"));
                        }

                        shpBasePath = shpFiles[0][..^4]; // remove .shp extension
                    }
                    catch (InvalidDataException)
                    {
                        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                        return BadRequest(ApiResponse<object>.ErrorResponse("ملف ZIP غير صالح"));
                    }
                    catch (Exception)
                    {
                        // Always clean up temp directory on any failure
                        if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
                        throw;
                    }
                }
                else
                {
                    // Direct .shp upload - save to storage
                    shpBasePath = Path.Combine(_storagePath, $"{fileType.ToString().ToLower()}");
                    var shpPath = shpBasePath + ".shp";
                    using (var stream = System.IO.File.Create(shpPath))
                    {
                        await file.CopyToAsync(stream);
                    }
                }

                // Deactivate old files of same type
                await _gisFiles.DeactivateByTypeAsync(fileType);

                storedFileName = $"{fileType.ToString().ToLower()}.shp";
                storedFilePath = Path.Combine(_storagePath, storedFileName);

                // Create database record
                var gisFile = new GisFile
                {
                    FileType = fileType,
                    OriginalFileName = file.FileName,
                    StoredFileName = storedFileName,
                    FileSize = file.Length,
                    IsActive = true,
                    FeaturesCount = 0,
                    UploadedByUserId = userId.Value,
                    UploadedAt = DateTime.UtcNow,
                    Notes = notes
                };

                await _gisFiles.AddAsync(gisFile);

                // Auto-import shapefile to zones
                if (autoImport)
                {
                    try
                    {
                        await _gisService.ImportShapefileAsync(shpBasePath + ".shp", user.MunicipalityId, fileType);

                        gisFile.LastImportedAt = DateTime.UtcNow;
                        await _gisFiles.UpdateAsync(gisFile);

                        _logger.LogInformation("Shapefile auto-imported to zones: {Type}", fileType);
                    }
                    catch (Exception importEx)
                    {
                        _logger.LogWarning(importEx, "Failed to auto-import Shapefile to zones");
                        return Ok(new
                        {
                            success = true,
                            message = "تم رفع الملف بنجاح، لكن فشل الاستيراد التلقائي. يمكنك الاستيراد يدوياً لاحقاً.",
                            warning = "فشل الاستيراد التلقائي",
                            data = MapToDto(gisFile)
                        });
                    }
                }

                _logger.LogInformation("Shapefile uploaded: {Type} by user {UserId}", fileType, userId);

                return Ok(new
                {
                    success = true,
                    message = autoImport
                        ? "تم رفع واستيراد ملف Shapefile بنجاح"
                        : "تم رفع ملف Shapefile بنجاح. يمكنك استيراده لاحقاً.",
                    data = MapToDto(gisFile)
                });
            }

            // Handle GeoJSON upload
            string jsonContent;
            using (var reader = new StreamReader(file.OpenReadStream(), System.Text.Encoding.UTF8))
            {
                jsonContent = await reader.ReadToEndAsync();
            }

            // Basic validation - check if it's valid JSON with features
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
                    return BadRequest(ApiResponse<object>.ErrorResponse("ملف GeoJSON لا يحتوي على أي features"));
                }
            }
            catch (JsonException)
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف JSON غير صالح"));
            }

            // Deactivate old files of same type
            await _gisFiles.DeactivateByTypeAsync(fileType);

            // Generate stored filename
            storedFileName = $"{fileType.ToString().ToLower()}.geojson";
            storedFilePath = Path.Combine(_storagePath, storedFileName);

            // Save new file (overwrite if exists, with retry for file locks)
            await WriteFileWithRetryAsync(storedFilePath, jsonContent);

            // Create database record
            var geoJsonFile = new GisFile
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

            await _gisFiles.AddAsync(geoJsonFile);

            _logger.LogInformation("GIS file uploaded: {Type} by user {UserId}, {Features} features",
                fileType, userId, featuresCount);

            // Auto-import to zones if requested
            if (autoImport)
            {
                try
                {
                    // Import directly from memory to avoid file lock issues
                    if (fileType == GisFileType.Blocks)
                    {
                        await _gisService.ImportBlocksFromGeoJsonStringAsync(jsonContent, user.MunicipalityId);
                    }
                    else
                    {
                        await _gisService.ImportGeoJsonStringAsync(jsonContent, user.MunicipalityId, fileType);
                    }

                    geoJsonFile.LastImportedAt = DateTime.UtcNow;
                    await _gisFiles.UpdateAsync(geoJsonFile);

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
                        warning = "فشل الاستيراد التلقائي",
                        data = MapToDto(geoJsonFile)
                    });
                }
            }

            return Ok(new
            {
                success = true,
                message = autoImport
                    ? $"تم رفع واستيراد الملف بنجاح ({featuresCount} منطقة)"
                    : $"تم رفع الملف بنجاح ({featuresCount} منطقة). يمكنك استيراده لاحقاً.",
                data = MapToDto(geoJsonFile)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload GIS file: {Message}", ex.Message);
            return BadRequest(ApiResponse<object>.ErrorResponse($"فشل رفع الملف: {ex.Message}"));
        }
    }

    [HttpPost("import/{fileId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "import gis file to zones")]
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
            return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود"));

        var safeFileName = Path.GetFileName(gisFile.StoredFileName);
        var filePath = Path.Combine(_storagePath, safeFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود على السيرفر"));

        try
        {
            if (gisFile.FileType == GisFileType.Blocks)
            {
                await _gisService.ImportBlocksFromGeoJsonAsync(filePath, user.MunicipalityId);
            }
            else
            {
                await _gisService.ImportGeoJsonAsync(filePath, user.MunicipalityId, gisFile.FileType);
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
            return BadRequest(ApiResponse<object>.ErrorResponse("فشل استيراد الملف. يرجى المحاولة مرة أخرى"));
        }
    }

    [HttpDelete("files/{fileId}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "delete a gis file")]
    public async Task<IActionResult> DeleteGisFile(int fileId)
    {
        var gisFile = await _gisFiles.GetByIdAsync(fileId);
        if (gisFile == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود"));

        try
        {
            // Delete physical file if it exists
            var safeFileName = Path.GetFileName(gisFile.StoredFileName);
            var filePath = Path.Combine(_storagePath, safeFileName);
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
            return BadRequest(ApiResponse<object>.ErrorResponse("فشل حذف الملف. يرجى المحاولة مرة أخرى"));
        }
    }

    [HttpGet("files/{fileId}/download")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "download a gis file")]
    public async Task<IActionResult> DownloadGisFile(int fileId)
    {
        var gisFile = await _gisFiles.GetByIdAsync(fileId);
        if (gisFile == null)
            return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود"));

        var safeFileName = Path.GetFileName(gisFile.StoredFileName);
        var filePath = Path.Combine(_storagePath, safeFileName);
        if (!System.IO.File.Exists(filePath))
            return NotFound(ApiResponse<object>.ErrorResponse("الملف غير موجود على السيرفر"));

        // Use FileShare.ReadWrite so downloads don't block uploads
        byte[] content;
        using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
        using (var ms = new MemoryStream())
        {
            await fs.CopyToAsync(ms);
            content = ms.ToArray();
        }
        return File(content, "application/geo+json", gisFile.OriginalFileName);
    }

    [HttpGet("zones-summary")]
    [Authorize(Roles = "Admin,Supervisor")]
    [SwaggerOperation(Summary = "get zones count grouped by type")]
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
                byType = zonesList.GroupBy(z => z.ZoneType?.ToString() ?? "غير محدد")
                    .Select(g => new { type = g.Key, count = g.Count() })
            }
        });
    }

    // Write file with retry to handle transient file locks from antivirus, indexers, etc.
    private static async System.Threading.Tasks.Task WriteFileWithRetryAsync(string path, string content, int maxRetries = 3)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                // Use FileStream with FileShare.None to get exclusive access, then write
                using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
                using var writer = new StreamWriter(fs, System.Text.Encoding.UTF8);
                await writer.WriteAsync(content);
                return;
            }
            catch (IOException) when (attempt < maxRetries)
            {
                await System.Threading.Tasks.Task.Delay(500 * (attempt + 1)); // 500ms, 1s, 1.5s
            }
        }

        // All retries exhausted — throw so the caller knows the file was not written
        throw new IOException($"Failed to write file after {maxRetries + 1} attempts: {Path.GetFileName(path)}");
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
