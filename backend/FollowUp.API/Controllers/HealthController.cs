using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FollowUp.Infrastructure.Data;

namespace FollowUp.API.Controllers;

// this controller check if api and database are working
[Route("api/[controller]")]
public class HealthController : BaseApiController
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
    [AllowAnonymous]
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

            return Ok(new
            {
                status = "صحي",
                message = "API يعمل وقاعدة البيانات متاحة",
                timestamp = DateTime.UtcNow,
                database = new { connected = true }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed with exception");
            return StatusCode(500, new
            {
                status = "غير صحي",
                message = "فشل فحص الصحة",
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
