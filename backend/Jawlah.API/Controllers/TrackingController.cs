using Jawlah.API.LiveTracking;
using Jawlah.Core.DTOs.Tracking;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
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
    private readonly ILocationHistoryRepository _history;
    private readonly IHubContext<TrackingHub, ITrackingClient> _hub;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ILocationHistoryRepository history,
        IHubContext<TrackingHub, ITrackingClient> hub,
        ILogger<TrackingController> logger)
    {
        _history = history;
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
    {
        // 1. get current user ID
        var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!int.TryParse(userIdStr, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            // 2. save the location to the history table
            var history = new LocationHistory
            {
                UserId = userId,
                Latitude = dto.Latitude,
                Longitude = dto.Longitude,
                Speed = dto.Speed,
                Accuracy = dto.Accuracy,
                Heading = dto.Heading,
                Timestamp = dto.Timestamp == default ? DateTime.UtcNow : dto.Timestamp,
                IsSync = true
            };

            await _history.AddAsync(history);
            await _history.SaveChangesAsync();

            // 3. tell the supervisor about the new location live
            var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
            await _hub.Clients.Group("Supervisors").ReceiveLocationUpdate(
                userId,
                userName,
                dto.Latitude,
                dto.Longitude,
                history.Timestamp);

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating location for user {UserId}", userId);
            return StatusCode(500, new { success = false, message = "حدث خطأ داخلي" });
        }
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(int userId, [FromQuery] DateTime? date)
    {
        // default to today if not provided
        var targetDate = date ?? DateTime.UtcNow.Date;
        var startOfDay = targetDate.Date;
        var endOfDay = targetDate.Date.AddDays(1).AddTicks(-1);

        var history = await _history.GetUserHistoryAsync(userId, startOfDay, endOfDay);

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
