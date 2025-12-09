using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Zones;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ZonesController : ControllerBase
{
    private readonly IZoneRepository _zoneRepo;
    private readonly JawlahDbContext _context;
    private readonly IGisService _gisService;
    private readonly ILogger<ZonesController> _logger;

    public ZonesController(IZoneRepository zoneRepo, JawlahDbContext context, IGisService gisService, ILogger<ZonesController> logger)
    {
        _zoneRepo = zoneRepo;
        _context = context;
        _gisService = gisService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllZones()
    {
        var zones = await _zoneRepo.GetActiveZonesAsync();
        return Ok(ApiResponse<IEnumerable<ZoneResponse>>.SuccessResponse(
            zones.Select(z => MapToZoneResponse(z))));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetZoneById(int id)
    {
        var zone = await _zoneRepo.GetByIdAsync(id);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Zone not found"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(MapToZoneResponse(zone)));
    }

    [HttpGet("by-code/{code}")]
    public async Task<IActionResult> GetZoneByCode(string code)
    {
        var zone = await _zoneRepo.GetByCodeAsync(code);
        if (zone == null)
            return NotFound(ApiResponse<object>.ErrorResponse("Zone not found"));

        return Ok(ApiResponse<ZoneResponse>.SuccessResponse(MapToZoneResponse(zone)));
    }

    [HttpPost("validate-location")]
    [HttpPost("validate")]  // Alias route for frontend compatibility
    public async Task<IActionResult> ValidateLocation([FromBody] ValidateLocationRequest request)
    {
        try
        {
            var zone = await _gisService.ValidateLocationAsync(request.Latitude, request.Longitude);

            if (zone == null)
            {
                return Ok(ApiResponse<ValidateLocationResponse>.SuccessResponse(
                    new ValidateLocationResponse
                    {
                        IsValid = false,
                        Message = "Location is outside all defined zones",
                        Zone = null
                    }));
            }

            return Ok(ApiResponse<ValidateLocationResponse>.SuccessResponse(
                new ValidateLocationResponse
                {
                    IsValid = true,
                    Message = $"Location is within {zone.ZoneName}",
                    Zone = MapToZoneResponse(zone)
                }));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse(ex.Message));
        }
    }

    // NOTE: Zone creation/editing is NOT part of the project scope.
    // Zones are imported from GIS shapefiles provided by the municipality.
    // Zone management is read-only + import functionality only.

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

            await _gisService.ImportShapefileAsync(request.FilePath);
            await _context.SaveChangesAsync();

            var zones = await _zoneRepo.GetActiveZonesAsync();
            var count = zones.Count();

            _logger.LogInformation("Shapefile import completed. Total zones: {Count}", count);

            return Ok(ApiResponse<object>.SuccessResponse(
                new { Message = $"Successfully imported zones from shapefile", TotalZones = count },
                "Shapefile imported successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import shapefile from: {FilePath}", request.FilePath);
            return StatusCode(500, ApiResponse<object>.ErrorResponse($"Failed to import shapefile: {ex.Message}"));
        }
    }

    private ZoneResponse MapToZoneResponse(Zone zone)
    {
        return new ZoneResponse
        {
            ZoneId = zone.ZoneId,
            ZoneName = zone.ZoneName,
            ZoneCode = zone.ZoneCode,
            Description = zone.Description,
            CenterLatitude = zone.CenterLatitude,
            CenterLongitude = zone.CenterLongitude,
            AreaSquareMeters = zone.AreaSquareMeters,
            District = zone.District,
            Version = zone.Version,
            IsActive = zone.IsActive,
            CreatedAt = zone.CreatedAt
        };
    }
}
