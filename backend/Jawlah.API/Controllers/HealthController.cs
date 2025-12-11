using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jawlah.Infrastructure.Data;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly JawlahDbContext _context;
    private readonly ILogger<HealthController> _logger;

    public HealthController(JawlahDbContext context, ILogger<HealthController> logger)
    {
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // Check database connectivity
            var canConnect = await _context.Database.CanConnectAsync();

            if (!canConnect)
            {
                _logger.LogWarning("Health check failed: Database connection failed");
                return StatusCode(503, new
                {
                    status = "Unhealthy",
                    message = "Database connection failed",
                    timestamp = DateTime.UtcNow
                });
            }

            // Get basic stats
            var userCount = await _context.Users.CountAsync();
            var taskCount = await _context.Tasks.CountAsync();
            var zoneCount = await _context.Zones.CountAsync();

            _logger.LogInformation("Health check passed");

            return Ok(new
            {
                status = "Healthy",
                message = "API is running and database is accessible",
                timestamp = DateTime.UtcNow,
                database = new
                {
                    connected = true,
                    users = userCount,
                    tasks = taskCount,
                    zones = zoneCount
                },
                version = "1.0.0"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(500, new
            {
                status = "Unhealthy",
                message = "Health check failed",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    [HttpGet("ping")]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        });
    }
}
