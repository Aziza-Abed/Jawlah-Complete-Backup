using AutoMapper;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Zones;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

// this controller handle zones and geofencing
[Route("api/[controller]")]
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

    // get zones assigned to current user
    [HttpGet("my")]
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
        var zones = user.AssignedZones.Select(uz => uz.Zone).Where(z => z.IsActive).ToList();

        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => _mapper.Map<ZoneResponse>(z))));
    }

    // get all active zones
    [HttpGet]
    public async Task<IActionResult> GetAllZones()
    {
        var zones = await _zones.GetActiveZonesAsync();
        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => _mapper.Map<ZoneResponse>(z))));
    }

    // get zone by id
    [HttpGet("{id}")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var zone = await _zones.GetByIdAsync(id);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone)));
    }

    // get zones as geojson for map
    [HttpGet("map-data")]
    public async Task<IActionResult> GetMapData()
    {
        var zones = await _zones.GetActiveZonesAsync();

        // convert to geojson format
        var features = zones.Select(z => new
        {
            type = "Feature",
            properties = new
            {
                zoneId = z.ZoneId,
                zoneName = z.ZoneName,
                zoneCode = z.ZoneCode,
                district = z.District,
                areaSquareMeters = z.AreaSquareMeters,
                centerLatitude = z.CenterLatitude,
                centerLongitude = z.CenterLongitude
            },
            geometry = string.IsNullOrEmpty(z.BoundaryGeoJson)
                ? null
                : System.Text.Json.JsonSerializer.Deserialize<object>(z.BoundaryGeoJson)
        }).ToList();

        var featureCollection = new
        {
            type = "FeatureCollection",
            features = features
        };

        return Ok(ApiResponse<object>.SuccessResponse(featureCollection));
    }

    // get zone by code
    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetZoneByCode(string code)
    {
        var zone = await _zones.GetByCodeAsync(code);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone)));
    }

    // validate if gps point is inside any zone
    [HttpPost("validate-location")]
    [HttpPost("validate")]
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
                    Message = $"Location is within {zone.ZoneName}",
                    Zone = _mapper.Map<ZoneResponse>(zone)
                }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    // import zones from shapefile admin only
    [HttpPost("import-shapefile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportShapefile([FromBody] ImportShapefileRequest request)
    {
        try
        {
            // check file path is provided
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("مسار الملف مطلوب"));
            }

            // check file exists
            if (!System.IO.File.Exists(request.FilePath))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"ملف الشكل غير موجود في: {request.FilePath}"));
            }

            _logger.LogInformation("Starting shapefile import from: {FilePath} for municipality {MunicipalityId}",
                request.FilePath, request.MunicipalityId);

            // do the import
            await _gis.ImportShapefileAsync(request.FilePath, request.MunicipalityId);

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
