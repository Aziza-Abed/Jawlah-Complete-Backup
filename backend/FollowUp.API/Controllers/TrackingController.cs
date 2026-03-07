using FollowUp.API.LiveTracking;
using FollowUp.Core.Constants;
using FollowUp.Core.DTOs.Common;
using FollowUp.Core.DTOs.Tracking;
using FollowUp.Core.Entities;
using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Swashbuckle.AspNetCore.Annotations;

namespace FollowUp.API.Controllers;

[Route("api/[controller]")]
[Tags("Tracking")]
public class TrackingController : BaseApiController
{
    private readonly ILocationHistoryRepository _history;
    private readonly IUserRepository _users;
    private readonly IHubContext<TrackingHub, ITrackingClient> _hub;
    private readonly ILogger<TrackingController> _logger;

    public TrackingController(
        ILocationHistoryRepository history,
        IUserRepository users,
        IHubContext<TrackingHub, ITrackingClient> hub,
        ILogger<TrackingController> logger)
    {
        _history = history;
        _users = users;
        _hub = hub;
        _logger = logger;
    }

    [HttpPost("location")]
    [SwaggerOperation(Summary = "update worker gps location")]
    public async Task<IActionResult> UpdateLocation([FromBody] LocationUpdateDto dto)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        // Validate GPS coordinates
        var gpsValidation = ValidateGpsCoordinates(dto.Latitude, dto.Longitude);
        if (gpsValidation != null)
            return gpsValidation;

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

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("locations")]
    [SwaggerOperation(Summary = "get latest location of all workers")]
    public async Task<IActionResult> GetWorkerLocations()
    {
        var now = DateTime.UtcNow;

        // get latest location for each worker (regardless of date, show last known position)
        var locations = await _history.GetLatestLocationsAsync(DateTime.MinValue);

        // Supervisors can only see their own workers' locations
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();
        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            var myWorkers = await _users.GetWorkersBySupervisorAsync(currentUserId.Value);
            var myWorkerIds = myWorkers.Select(w => w.UserId).ToHashSet();
            locations = locations.Where(loc => myWorkerIds.Contains(loc.UserId));
        }

        // map to response with worker info
        // Use both SignalR connection state AND timestamp to determine online status
        var result = locations.Select(loc =>
        {
            var recentLocation = (now - loc.Timestamp).TotalMinutes <= AppConstants.OnlineThresholdMinutes;
            var hubConnected = TrackingHub.IsUserConnected(loc.UserId);
            var isOnline = recentLocation || hubConnected;
            return new
            {
                UserId = loc.UserId,
                FullName = loc.User?.FullName ?? "غير معروف",
                Username = loc.User?.Username,
                Latitude = loc.Latitude,
                Longitude = loc.Longitude,
                Speed = loc.Speed,
                Accuracy = loc.Accuracy,
                Timestamp = loc.Timestamp,
                IsOnline = isOnline,
                Status = isOnline ? "Online" : "Offline",
                ZoneName = loc.User?.AssignedZones?.FirstOrDefault()?.Zone?.ZoneName
            };
        });

        return Ok(ApiResponse<object>.SuccessResponse(result));
    }

    [Authorize(Roles = "Admin,Supervisor")]
    [HttpGet("history/{userId}")]
    [SwaggerOperation(Summary = "get location history for a worker")]
    public async Task<IActionResult> GetHistory(int userId, [FromQuery] DateTime? date)
    {
        // Supervisors can only view their own workers' history
        var currentRole = GetCurrentUserRole();
        var currentUserId = GetCurrentUserId();
        if (currentRole == "Supervisor" && currentUserId.HasValue)
        {
            var worker = await _users.GetByIdAsync(userId);
            if (worker?.SupervisorId != currentUserId.Value)
                return Forbid();
        }

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
