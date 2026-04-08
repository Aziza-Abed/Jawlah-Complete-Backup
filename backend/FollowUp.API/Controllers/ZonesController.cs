using AutoMapper;
using FollowUp.API.Utils;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Zones;
using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using FollowUp.Core.Interfaces.Repositories;
using FollowUp.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Zones")]
public class ZonesController : BaseApiController
{
    private readonly IZoneRepository _zones;
    private readonly IUserRepository _users;
    private readonly IGisService _gis;
    private readonly ILogger<ZonesController> _logger;
    private readonly IMapper _mapper;

    public ZonesController(IZoneRepository zones, IUserRepository users, IGisService gis, ILogger<ZonesController> logger, IMapper mapper)
    {
        _zones = zones;
        _users = users;
        _gis = gis;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet("my")]
    [SwaggerOperation(Summary = "get zones assigned to current user")]
    public async Task<IActionResult> GetMyZones()
    {
        // get user id
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // get user with zones
        var user = await _users.GetUserWithZonesAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // extract active zones
        var zones = user.AssignedZones.Select(uz => uz.Zone).Where(z => z != null && z.IsActive).ToList();

        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => _mapper.Map<ZoneResponse>(z))));
    }

    [HttpGet]
    [SwaggerOperation(Summary = "get all active zones")]
    public async Task<IActionResult> GetAllZones([FromQuery] string? type = null)
    {
        var zones = await _zones.GetActiveZonesAsync();

        if (!string.IsNullOrEmpty(type) && Enum.TryParse<GisFileType>(type, true, out var zoneType))
            zones = zones.Where(z => z.ZoneType == zoneType);

        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => _mapper.Map<ZoneResponse>(z))));
    }

    [HttpGet("{id}")]
    [SwaggerOperation(Summary = "get zone by id")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var zone = await _zones.GetByIdAsync(id);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone)));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "create a new zone")]
    public async Task<IActionResult> CreateZone([FromBody] CreateZoneRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("بيانات المنطقة غير صالحة"));

        try
        {
            // Get municipality ID from current user
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
                return Unauthorized(ApiResponse<object>.ErrorResponse("غير مصرح"));

            var user = await _users.GetByIdAsync(userId.Value);
            if (user == null)
                return Unauthorized(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

            // Check if zone code already exists in this municipality
            var existingZone = await _zones.GetByCodeAndMunicipalityAsync(request.ZoneCode, user.MunicipalityId);
            if (existingZone != null)
                return BadRequest(ApiResponse<object>.ErrorResponse($"كود المنطقة '{request.ZoneCode}' موجود مسبقاً"));

            var zone = new Zone
            {
                MunicipalityId = user.MunicipalityId,
                ZoneName = InputSanitizer.SanitizeString(request.ZoneName, 100),
                ZoneCode = request.ZoneCode.Trim(),
                Description = InputSanitizer.SanitizeString(request.Description, 500),
                AreaSquareMeters = request.AreaSquareMeters,
                BoundaryGeoJson = request.BoundaryGeoJson,
                District = InputSanitizer.SanitizeString(request.District, 100),
                ZoneType = !string.IsNullOrEmpty(request.ZoneType) && Enum.TryParse<GisFileType>(request.ZoneType, true, out var zt) ? zt : null,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                Version = 1,
                VersionDate = DateTime.UtcNow,
                VersionNotes = "تم إنشاء المنطقة"
            };

            // Parse GeoJSON to set center coordinates and Boundary geometry
            if (!string.IsNullOrEmpty(request.BoundaryGeoJson))
                ApplyGeoJsonToZone(zone, request.BoundaryGeoJson, calculateArea: true);

            await _zones.AddAsync(zone);
            await _zones.SaveChangesAsync();

            _logger.LogInformation("Zone {ZoneCode} created by user {UserId}", zone.ZoneCode, userId.Value);
            return CreatedAtAction(nameof(GetZoneById), new { id = zone.ZoneId },
                ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone), "تم إنشاء المنطقة بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create zone");
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل إنشاء المنطقة"));
        }
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "update an existing zone")]
    public async Task<IActionResult> UpdateZone(int id, [FromBody] UpdateZoneRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("بيانات المنطقة غير صالحة"));

        try
        {
            var zone = await _zones.GetByIdAsync(id);
            if (zone == null)
                return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

            // Check if zone code changed and already exists
            if (!string.IsNullOrEmpty(request.ZoneCode) && request.ZoneCode != zone.ZoneCode)
            {
                var existingZone = await _zones.GetByCodeAndMunicipalityAsync(request.ZoneCode, zone.MunicipalityId);
                if (existingZone != null && existingZone.ZoneId != id)
                    return BadRequest(ApiResponse<object>.ErrorResponse($"كود المنطقة '{request.ZoneCode}' موجود مسبقاً"));
                zone.ZoneCode = request.ZoneCode;
            }

            if (!string.IsNullOrEmpty(request.ZoneName))
                zone.ZoneName = InputSanitizer.SanitizeString(request.ZoneName, 100);

            if (request.Description != null)
                zone.Description = InputSanitizer.SanitizeString(request.Description, 500);

            if (request.District != null)
                zone.District = InputSanitizer.SanitizeString(request.District, 100);

            if (request.AreaSquareMeters.HasValue)
                zone.AreaSquareMeters = request.AreaSquareMeters.Value;

            if (request.ZoneType != null)
            {
                if (Enum.TryParse<GisFileType>(request.ZoneType, true, out var zt))
                    zone.ZoneType = zt;
                else if (string.IsNullOrEmpty(request.ZoneType))
                    zone.ZoneType = null;
            }

            // Update GeoJSON and geometry
            if (!string.IsNullOrEmpty(request.BoundaryGeoJson))
            {
                zone.BoundaryGeoJson = request.BoundaryGeoJson;
                ApplyGeoJsonToZone(zone, request.BoundaryGeoJson);
            }

            zone.UpdatedAt = DateTime.UtcNow;
            zone.Version++;
            zone.VersionDate = DateTime.UtcNow;
            zone.VersionNotes = "تم تحديث المنطقة";

            await _zones.UpdateAsync(zone);
            await _zones.SaveChangesAsync();

            _logger.LogInformation("Zone {ZoneId} updated by user {UserId}", id, GetCurrentUserId());
            return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone), "تم تحديث المنطقة بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update zone {ZoneId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل تحديث المنطقة"));
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "soft delete a zone")]
    public async Task<IActionResult> DeleteZone(int id)
    {
        try
        {
            var zone = await _zones.GetByIdAsync(id);
            if (zone == null)
                return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

            zone.IsActive = false;
            zone.UpdatedAt = DateTime.UtcNow;
            await _zones.UpdateAsync(zone);
            await _zones.SaveChangesAsync();

            _logger.LogInformation("Zone {ZoneId} soft deleted by user {UserId}", id, GetCurrentUserId());
            return Ok(ApiResponse<object>.SuccessResponse(new { }, "تم حذف المنطقة بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete zone {ZoneId}", id);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل حذف المنطقة"));
        }
    }

    [HttpGet("map-data")]
    [ResponseCache(NoStore = true)]
    [SwaggerOperation(Summary = "get zones as geojson for map")]
    public async Task<IActionResult> GetMapData()
    {
        var zones = await _zones.GetActiveZonesAsync();

        // convert to geojson format
        var features = zones.Select(z =>
        {
            object? geometry = null;

            // Try to parse BoundaryGeoJson if it exists and looks like JSON
            if (!string.IsNullOrEmpty(z.BoundaryGeoJson) && z.BoundaryGeoJson.TrimStart().StartsWith("{"))
            {
                try
                {
                    geometry = System.Text.Json.JsonSerializer.Deserialize<object>(z.BoundaryGeoJson);
                }
                catch
                {
                    // Skip invalid GeoJSON
                    geometry = null;
                }
            }

            return new
            {
                type = "Feature",
                properties = new
                {
                    zoneId = z.ZoneId,
                    zoneName = z.ZoneName,
                    zoneCode = z.ZoneCode,
                    district = z.District,
                    zoneType = z.ZoneType?.ToString(),
                    areaSquareMeters = z.AreaSquareMeters,
                    centerLatitude = z.CenterLatitude,
                    centerLongitude = z.CenterLongitude
                },
                geometry
            };
        }).ToList();

        var featureCollection = new
        {
            type = "FeatureCollection",
            features = features
        };

        return Ok(ApiResponse<object>.SuccessResponse(featureCollection));
    }

    [HttpGet("map-markers")]
    [ResponseCache(Duration = 3600, Location = ResponseCacheLocation.Client)]
    [SwaggerOperation(Summary = "get zone markers for map")]
    public async Task<IActionResult> GetMapMarkers()
    {
        var zones = await _zones.GetActiveZonesAsync();

        var markers = zones.Select(z => new
        {
            zoneId = z.ZoneId,
            zoneName = z.ZoneName,
            zoneCode = z.ZoneCode,
            latitude = z.CenterLatitude,
            longitude = z.CenterLongitude,
            district = z.District,
            areaSquareMeters = z.AreaSquareMeters
        }).ToList();

        return Ok(ApiResponse<object>.SuccessResponse(new { markers }));
    }

    [HttpGet("by-code/{code}")]
    [SwaggerOperation(Summary = "get zone by code")]
    public async Task<IActionResult> GetZoneByCode(string code)
    {
        var zone = await _zones.GetByCodeAsync(code);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone)));
    }

    [HttpPost("validate-location")]
    [HttpPost("validate")]
    [SwaggerOperation(Summary = "validate if location is inside a zone")]
    public async Task<IActionResult> ValidateLocation([FromBody] ValidateLocationRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(ApiResponse<object>.ErrorResponse("بيانات الموقع غير صالحة"));

        try
        {
            // check if point is in any zone
            var zone = await _gis.ValidateLocationAsync(request.Latitude, request.Longitude);

            if (zone == null)
            {
                return Ok(ApiResponse<ValidateLocationResponse>.SuccessResponse(
                    new ValidateLocationResponse
                    {
                        IsValid = false,
                        Message = "الموقع خارج جميع المناطق المحددة",
                        Zone = null
                    }));
            }

            return Ok(ApiResponse<ValidateLocationResponse>.SuccessResponse(
                new ValidateLocationResponse
                {
                    IsValid = true,
                    Message = $"الموقع داخل منطقة {zone.ZoneName}",
                    Zone = _mapper.Map<ZoneResponse>(zone)
                }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    // helper: parse GeoJSON and apply geometry/centroid to zone
    private void ApplyGeoJsonToZone(Zone zone, string geoJson, bool calculateArea = false)
    {
        try
        {
            var geometry = _gis.ParseGeoJson(geoJson);
            if (geometry != null)
            {
                zone.Boundary = geometry;
                var centroid = geometry.Centroid;
                zone.CenterLatitude = centroid.Y;
                zone.CenterLongitude = centroid.X;
                if (calculateArea)
                    zone.AreaSquareMeters = geometry.Area * 111319.9 * 111319.9 * Math.Cos(centroid.Y * Math.PI / 180);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse GeoJSON for zone {ZoneCode}", zone.ZoneCode);
        }
    }

    [HttpPost("import-shapefile")]
    [Authorize(Roles = "Admin")]
    [SwaggerOperation(Summary = "import zones from shapefile")]
    public async Task<IActionResult> ImportShapefile([FromBody] ImportShapefileRequest request)
    {
        try
        {
            // check file path is provided
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("مسار الملف مطلوب"));
            }

            // restrict access to Storage/GIS dir only
            var allowedBasePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "Storage", "GIS"));
            var resolvedPath = Path.GetFullPath(request.FilePath);
            if (!resolvedPath.StartsWith(allowedBasePath, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Shapefile import blocked - path outside Storage/GIS: {FilePath}", request.FilePath);
                return BadRequest(ApiResponse<object>.ErrorResponse("مسار الملف غير مسموح به. يجب أن يكون داخل مجلد Storage/GIS"));
            }

            // check file exists
            if (!System.IO.File.Exists(resolvedPath))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("ملف الشكل غير موجود"));
            }

            _logger.LogInformation("Starting shapefile import from: {FilePath} for municipality {MunicipalityId}",
                resolvedPath, request.MunicipalityId);

            // do the import
            await _gis.ImportShapefileAsync(resolvedPath, request.MunicipalityId);

            // get count of zones
            var zones = await _zones.GetActiveZonesAsync();
            var count = zones.Count();

            _logger.LogInformation("Shapefile import completed. Total zones: {Count}", count);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { Message = "تم استيراد المناطق بنجاح", TotalZones = count },
                "تم استيراد ملف الشكل بنجاح"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import shapefile from: {FilePath}", request.FilePath);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("فشل استيراد ملف الشكل. يرجى التحقق من تنسيق الملف والمحاولة مرة أخرى."));
        }
    }
}
