using AutoMapper;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Zones;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

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

    [HttpGet("my")]
    public async Task<IActionResult> GetMyZones()
    {
        // get the current user ID
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // find the user and his assigned zones
        var user = await _users.GetUserWithZonesAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        // extract the zone list for the worker
        var zones = user.AssignedZones.Select(uz => uz.Zone).Where(z => z.IsActive).ToList();

        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => _mapper.Map<ZoneResponse>(z))));
    }

    [HttpGet]
    public async Task<IActionResult> GetAllZones()
    {
        var zones = await _zones.GetActiveZonesAsync();
        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => _mapper.Map<ZoneResponse>(z))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var zone = await _zones.GetByIdAsync(id);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone)));
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetZoneByCode(string code)
    {
        var zone = await _zones.GetByCodeAsync(code);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المنطقة غير موجودة"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(_mapper.Map<ZoneResponse>(zone)));
    }

    [HttpPost("validate-location")]
    [HttpPost("validate")]  // Alias route for frontend compatibility
    public async Task<IActionResult> ValidateLocation([FromBody] ValidateLocationRequest request)
    {
        try
        {
            // check if the GPS point is inside any zone
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

    // zones are imported from shapefiles only (read-only + import)
    [HttpPost("import-shapefile")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ImportShapefile([FromBody] ImportShapefileRequest request)
    {
        try
        {
            if (string.IsNullOrEmpty(request.FilePath))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse("File path is required"));
            }

            if (!System.IO.File.Exists(request.FilePath))
            {
                return BadRequest(ApiResponse<object>.ErrorResponse($"Shapefile not found at: {request.FilePath}"));
            }

            _logger.LogInformation("Starting shapefile import from: {FilePath}", request.FilePath);

            await _gis.ImportShapefileAsync(request.FilePath);
            await _zones.SaveChangesAsync();

            var zones = await _zones.GetActiveZonesAsync();
            var count = zones.Count();

            _logger.LogInformation("Shapefile import completed. Total zones: {Count}", count);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { Message = $"Successfully imported zones from shapefile", TotalZones = count },
                "Shapefile imported successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import shapefile from: {FilePath}", request.FilePath);
            return StatusCode(500, ApiResponse<object>.ErrorResponse("Failed to import shapefile. Please check the file format and try again."));
        }
    }
}
