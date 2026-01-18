using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class GisController : BaseApiController
{
    private readonly IGisService _gisService;
    private readonly IUserRepository _users;
    private readonly ILogger<GisController> _logger;

    public GisController(
        IGisService gisService,
        IUserRepository users,
        ILogger<GisController> logger)
    {
        _gisService = gisService;
        _users = users;
        _logger = logger;
    }

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

    [HttpPost("import-shapefile")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> ImportShapefile([FromBody] GisImportShapefileRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return Unauthorized();

        try
        {
            await _gisService.ImportShapefileAsync(
                request.FilePath,
                user.MunicipalityId
            );

            return Ok(new
            {
                success = true,
                message = "Shapefile imported successfully"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import shapefile");
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
