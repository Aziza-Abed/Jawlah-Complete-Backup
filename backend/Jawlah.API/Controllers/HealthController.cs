using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Jawlah.Infrastructure.Data;

namespace Jawlah.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    private readonly JawlahDbContext _db;
    private readonly ILogger<HealthController> _logger;

    public HealthController(JawlahDbContext db, ILogger<HealthController> logger)
    {
        _db = db;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        try
        {
            // 1. check database connection (simple SELECT 1 check)
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

            // 2. count important things to show the system is alive
            // Note: We use sequential calls because DbContext is not thread-safe
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
