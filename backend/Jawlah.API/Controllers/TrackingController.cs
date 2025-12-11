using Jawlah.API.LiveTracking;
using Jawlah.Core.DTOs.Tracking;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Jawlah.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TrackingController : ControllerBase
{
    private readonly ILocationHistoryRepository _locationHistoryRepo;
    private readonly JawlahDbContext _context;
    private readonly IHubContext<TrackingHub, ITrackingClient> _hubContext;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ILocationHistoryRepository locationHistoryRepo,
        JawlahDbContext context,
        IHubContext<TrackingHub, ITrackingClient> hubContext,
        ILogger<TrackingController> logger)
    {
        _locationHistoryRepo = locationHistoryRepo;
        _context = context;
        _hubContext = hubContext;
        _logger = logger;
    }

    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
    {
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            // 1. Store in Database
            var history = new LocationHistory
            {
                UserId = userId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Speed = dto.Speed,
                Accuracy = dto.Accuracy,
                Heading = dto.Heading,
                Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp,
                IsSync = true // Coming from offline batch or direct API
            };

            await _locationHistoryRepo.AddAsync(history);
            await _context.SaveChangesAsync();

            // 2. Broadcast via SignalR (optional, if we want REST updates to also move the map)
            // Useful if the client falls back to REST but we still want live updates if possible
            await _hubContext.Clients.Group("Supervisors").ReceiveLocationUpdate(userId, dto.Latitude, dto.Longitude);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "Internal Server Error" });
        }
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(int userId, [FromQuery] DateTime? date)
    {
        // Default to today if not provided
        var targetDate = date ?? DateTime.UtcNow.Date;
        var startOfDay = targetDate.Date;
        var endOfDay = targetDate.Date.AddDays(1).AddTicks(-1);

        var history = await _locationHistoryRepo.GetUserHistoryAsync(userId, startOfDay, endOfDay);

        var dtos = history.Select(x => new LocationUpdateDto
        {
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Speed = x.Speed,
            Accuracy = x.Accuracy,
            Heading = x.Heading,
            Timestamp = x.Timestamp
        });

        return Ok(new { success = true, data = dtos });
    }
}
