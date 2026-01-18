using Jawlah.API.LiveTracking;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Tracking;
using Jawlah.Core.Entities;
using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace Jawlah.API.Controllers;

// this controller handle gps tracking for workers
[Route("api/[controller]")]
public class TrackingController : BaseApiController
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

    // save worker location and send to supervisor
    [HttpPost("location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // check gps is not zero
        if (dto.Latitude == 0 && dto.Longitude == 0)
            return BadRequest(ApiResponse<object>.ErrorResponse("إحداثيات GPS غير صالحة (0, 0). يرجى التأكد من تفعيل GPS"));

        // check coords are inside work area
        if (dto.Latitude < Core.Constants.GeofencingConstants.MinLatitude ||
            dto.Latitude > Core.Constants.GeofencingConstants.MaxLatitude ||
            dto.Longitude < Core.Constants.GeofencingConstants.MinLongitude ||
            dto.Longitude > Core.Constants.GeofencingConstants.MaxLongitude)
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("أنت خارج منطقة العمل المسموح بها"));
        }

        // save location to history table
        var history = new LocationHistory
        {
            UserId = userId.Value,
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

        // send location to supervisors in realtime
        var userName = User.Identity?.Name ?? "Unknown";
        await _hub.Clients.Group("Supervisors").ReceiveLocationUpdate(
            userId.Value,
            userName,
            dto.Latitude,
            dto.Longitude,
            history.Timestamp);

        return Ok(ApiResponse<object?>.SuccessResponse(null));
    }

    // get current locations for all workers (for live map)
    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("locations")]
    public async Task<IActionResult> GetWorkerLocations()
    {
        var today = DateTime.UtcNow.Date;
        var now = DateTime.UtcNow;

        // get latest location for each worker (regardless of date, show last known position)
        var locations = await _history.GetLatestLocationsAsync(DateTime.MinValue);

        // map to response with worker info
        var result = locations.Select(loc => new
        {
            UserId = loc.UserId,
            FullName = loc.User?.FullName ?? "غير معروف",
            Username = loc.User?.Username,
            Latitude = loc.Latitude,
            Longitude = loc.Longitude,
            Speed = loc.Speed,
            Accuracy = loc.Accuracy,
            Timestamp = loc.Timestamp,
            // consider online if location updated in last 15 minutes
            IsOnline = (now - loc.Timestamp).TotalMinutes <= 15,
            Status = (now - loc.Timestamp).TotalMinutes <= 15 ? "Online" : "Offline",
            ZoneName = loc.User?.AssignedZones?.FirstOrDefault()?.Zone?.ZoneName
        });

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    // get location history for worker
    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("history/{userId}")]
    public async Task<IActionResult> GetHistory(int userId, [FromQuery] DateTime? date)
    {
        // use today if no date given
        var targetDate = date ?? DateTime.UtcNow.Date;
        var startOfDay = targetDate.Date;
        var endOfDay = targetDate.Date.AddDays(1).AddTicks(-1);

        var history = await _history.GetUserHistoryAsync(userId, startOfDay, endOfDay);

        // map to dto
        var dtos = history.Select(x => new LocationUpdateDto
        {
            Latitude = x.Latitude,
            Longitude = x.Longitude,
            Speed = x.Speed,
            Accuracy = x.Accuracy,
            Heading = x.Heading,
            Timestamp = x.Timestamp
        });

        return Ok(ApiResponse<IEnumerable<LocationUpdateDto>>.SuccessResponse(dtos));
    }
}
