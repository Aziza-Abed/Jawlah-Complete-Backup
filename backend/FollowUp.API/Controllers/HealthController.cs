using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FollowUp.Infrastructure.Data;

namespace FollowUp.API.Controllers;

// this controller check if api and database are working
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HealthController : ControllerBase
{
    private readonly FollowUpDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(FollowUpDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    // main health check endpoint
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // check if we can connect to database
            var canConnect = await _db.Database.CanConnectAsync();

            if (!canConnect)
            {
                return StatusCode(503, new
                {
                    status = "غير صحي",
                    message = "فشل الاتصال بقاعدة البيانات",
                    timestamp = DateTime.UtcNow
                });
            }

            // count some records to show system is working
            var userCount = await _db.Users.CountAsync();
            var taskCount = await _db.Tasks.CountAsync();
            var zoneCount = await _db.Zones.CountAsync();

            return Ok(new
            {
                status = "صحي",
                message = "API يعمل وقاعدة البيانات متاحة",
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
                status = "غير صحي",
                message = "فشل فحص الصحة",
                error = ex.Message,
                timestamp = DateTime.UtcNow
            });
        }
    }

    // simple ping endpoint - public for load balancer health checks
    [HttpGet("ping")]
    [AllowAnonymous]
    public IActionResult Ping()
    {
        return Ok(new
        {
            message = "pong",
            timestamp = DateTime.UtcNow
        });
    }
}
