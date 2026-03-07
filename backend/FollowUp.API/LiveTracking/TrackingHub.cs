using FollowUp.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace FollowUp.API.LiveTracking;

// interface that defines what methods the client (mobile/web) should have
// SignalR will call these methods on the client side
public interface ITrackingClient
{
    // receive real-time location update from a worker
    Task ReceiveLocationUpdate(int userId, string userName, double latitude, double longitude, DateTime timestamp);

    // receive user online/offline status change
    Task ReceiveUserStatus(int userId, string userName, string status);

    // receive worker activity notification (started task, completed task, etc.)
    Task ReceiveActivity(int userId, string userName, string activityType, string description);

    // receive notification when worker enters/exits a zone
    Task ReceiveZoneEvent(int userId, string userName, int zoneId, string zoneName, string eventType);

    // receive connection statistics update
    Task ReceiveConnectionStats(int totalConnections, int workersOnline, int supervisorsOnline);
}

// connection tracking metadata
public class UserConnection
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string ConnectionId { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastActivity { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
}

// real-time tracking hub for location updates between workers and supervisors
[Authorize]
public class TrackingHub : Hub<ITrackingClient>
{
    private readonly ILogger<TrackingHub> _logger;
    private readonly ILocationHistoryRepository _locationHistory;

    // thread-safe dictionary to track active connections (SignalR is concurrent)
    private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();

    public TrackingHub(ILogger<TrackingHub> logger, ILocationHistoryRepository locationHistory)
    {
        _logger = logger;
        _locationHistory = locationHistory;
    }

    #region Connection Lifecycle

    // called when a client connects to the hub
    public override async Task OnConnectedAsync()
    {
        var (userId, userName, role) = GetCurrentHubUser();

        if (userId.HasValue)
        {
            var connection = new UserConnection
            {
                UserId = userId.Value,
                UserName = userName,
                Role = role,
                ConnectionId = Context.ConnectionId,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _connections[Context.ConnectionId] = connection;

            // put the user in groups based on their role
            if (role is "Admin" or "Supervisor")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Supervisors");
            }
            else if (role == "Worker")
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, "Workers");
            }

            // if it's a worker, tell supervisors they are online
            if (role == "Worker")
            {
                await Clients.Group("Supervisors").ReceiveUserStatus(
                    userId.Value, userName, "online");
            }

            await BroadcastConnectionStats();
            _logger.LogInformation("User connected: {UserName}", userName);
        }

        await base.OnConnectedAsync();
    }

    // called when a client disconnects from the hub
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        // remove the user from our connection list
        if (_connections.TryRemove(Context.ConnectionId, out var connection))
        {
            // tell supervisors that this person went offline
            if (connection.Role == "Worker")
            {
                await Clients.Group("Supervisors").ReceiveUserStatus(
                    connection.UserId,
                    connection.UserName,
                    "offline");
            }

            // update connection stats for everyone
            await BroadcastConnectionStats();

            _logger.LogInformation("User disconnected: {UserName}", connection.UserName);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Location Tracking

    // worker sends real-time location update
    public async Task SendLocationUpdate(double latitude, double longitude,
        double? accuracy = null, double? speed = null, double? heading = null)
    {
        var (userId, userName, _) = GetCurrentHubUser();

        if (!userId.HasValue)
        {
            _logger.LogWarning("Location update received from unauthenticated user");
            return;
        }

        if (!IsValidGpsCoordinate(latitude, longitude))
        {
            _logger.LogWarning("Invalid GPS coordinates from user {UserId}: {Lat}, {Lng}", userId, latitude, longitude);
            return;
        }

        var timestamp = DateTime.UtcNow;

        // update the connection info in our memory
        if (_connections.TryGetValue(Context.ConnectionId, out var conn))
        {
            conn.LastLatitude = latitude;
            conn.LastLongitude = longitude;
            conn.LastActivity = timestamp;
        }

        // save to database so REST API can retrieve locations (including accuracy/speed/heading for quality analysis)
        try
        {
            var history = new Core.Entities.LocationHistory
            {
                UserId = userId.Value,
                Latitude = latitude,
                Longitude = longitude,
                Accuracy = accuracy,
                Speed = speed,
                Heading = heading,
                Timestamp = timestamp,
                IsSync = true
            };
            await _locationHistory.AddAsync(history);
            await _locationHistory.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save location to database for user {UserId}", userId);
        }

        // send the new location to all supervisors so they can see it on the map
        await Clients.Group("Supervisors").ReceiveLocationUpdate(
            userId.Value, userName, latitude, longitude, timestamp);

        _logger.LogDebug("Location updated for {UserName}", userName);
    }

    // worker sends batch location updates (for offline sync)
    public async Task SendLocationBatch(List<LocationUpdate> locations)
    {
        var (userId, userName, _) = GetCurrentHubUser();

        if (!userId.HasValue || locations == null || !locations.Any())
            return;

        // Cap batch size to prevent memory exhaustion
        if (locations.Count > 500)
            locations = locations.Take(500).ToList();

        // sort once, filter invalid coordinates
        var validLocations = locations
            .Where(l => IsValidGpsCoordinate(l.Latitude, l.Longitude))
            .OrderBy(l => l.Timestamp)
            .ToList();

        if (!validLocations.Any())
            return;

        _logger.LogInformation("Received {Count} batch location updates from {UserName}", validLocations.Count, userName);

        // save batch locations to database
        try
        {
            foreach (var location in validLocations)
            {
                var history = new Core.Entities.LocationHistory
                {
                    UserId = userId.Value,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    Timestamp = location.Timestamp,
                    IsSync = true
                };
                await _locationHistory.AddAsync(history);
            }
            await _locationHistory.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save batch locations to database for user {UserId}", userId);
        }

        // broadcast each location to supervisors
        foreach (var location in validLocations)
        {
            await Clients.Group("Supervisors").ReceiveLocationUpdate(
                userId.Value, userName, location.Latitude, location.Longitude, location.Timestamp);
        }
    }

    #endregion

    #region Activity Notifications

    // worker notifies supervisors of an activity (task started, completed, etc.)
    public async Task NotifyActivity(string activityType, string description)
    {
        var (userId, userName, _) = GetCurrentHubUser();

        if (!userId.HasValue)
            return;

        await Clients.Group("Supervisors").ReceiveActivity(
            userId.Value, userName, activityType, description);

        _logger.LogInformation("Activity reported: {ActivityType} by {UserName}", activityType, userName);
    }

    // worker notifies when entering/exiting a zone
    public async Task NotifyZoneEvent(int zoneId, string zoneName, string eventType)
    {
        var (userId, userName, _) = GetCurrentHubUser();

        if (!userId.HasValue)
            return;

        // broadcast to supervisors
        await Clients.Group("Supervisors").ReceiveZoneEvent(
            userId.Value, userName, zoneId, zoneName, eventType);

        // also broadcast to other workers in the same zone (for coordination)
        await Clients.Group($"Zone_{zoneId}").ReceiveZoneEvent(
            userId.Value, userName, zoneId, zoneName, eventType);

        _logger.LogInformation("Zone event: {EventType} at {ZoneName}", eventType, zoneName);
    }

    #endregion

    #region Group Management

    // supervisor explicitly joins supervisors group to receive worker updates
    public async Task JoinSupervisorsGroup()
    {
        var (_, _, role) = GetCurrentHubUser();

        if (role is "Admin" or "Supervisor")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Supervisors");
            _logger.LogInformation("Supervisor joined tracking group");
        }
    }

    // worker joins a zone-specific group for zone-based notifications
    public async Task JoinZoneGroup(int zoneId)
    {
        var (_, _, role) = GetCurrentHubUser();

        if (role == "Worker")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
            _logger.LogInformation("Worker joined Zone group: {ZoneId}", zoneId);
        }
    }

    // worker leaves a zone-specific group
    public async Task LeaveZoneGroup(int zoneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
        _logger.LogInformation("Worker left Zone group: {ZoneId}", zoneId);
    }

    #endregion

    #region Connection Stats & Monitoring

    // get current connection statistics
    public ConnectionStats GetConnectionStats()
    {
        return CalculateConnectionStats();
    }

    // get list of currently online workers (for supervisors)
    public List<OnlineWorker> GetOnlineWorkers()
    {
        var (_, _, role) = GetCurrentHubUser();

        if (role is not "Admin" and not "Supervisor")
        {
            return new List<OnlineWorker>();
        }

        return _connections.Values
            .Where(c => c.Role == "Worker")
            .Select(c => new OnlineWorker
            {
                UserId = c.UserId,
                UserName = c.UserName,
                ConnectedAt = c.ConnectedAt,
                LastActivity = c.LastActivity,
                LastLatitude = c.LastLatitude,
                LastLongitude = c.LastLongitude
            })
            .ToList();
    }

    // heartbeat to keep connection alive and update last activity
    public Task Heartbeat()
    {
        // just update when we last heard from the client
        if (_connections.TryGetValue(Context.ConnectionId, out var conn))
        {
            conn.LastActivity = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    // Check if a user is currently connected via SignalR
    public static bool IsUserConnected(int userId)
        => _connections.Values.Any(c => c.UserId == userId);

    #endregion

    #region Private Helper Methods

    private (int? UserId, string UserName, string Role) GetCurrentHubUser()
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value ?? "Unknown";
        return (userId, userName, role);
    }

    private static bool IsValidGpsCoordinate(double latitude, double longitude)
        => latitude is >= -90 and <= 90 && longitude is >= -180 and <= 180
           && !(latitude == 0 && longitude == 0);

    private async Task BroadcastConnectionStats()
    {
        var stats = CalculateConnectionStats();

        await Clients.Group("Supervisors").ReceiveConnectionStats(
            stats.TotalConnections,
            stats.WorkersOnline,
            stats.SupervisorsOnline);
    }

    private ConnectionStats CalculateConnectionStats()
    {
        var connections = _connections.Values.ToList();

        return new ConnectionStats
        {
            TotalConnections = connections.Count,
            WorkersOnline = connections.Count(c => c.Role == "Worker"),
            SupervisorsOnline = connections.Count(c => c.Role == "Admin" || c.Role == "Supervisor"),
            AdminsOnline = connections.Count(c => c.Role == "Admin")
        };
    }

    #endregion
}

#region DTOs (Data Transfer Objects)

// location update for batch sync
public class LocationUpdate
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime Timestamp { get; set; }
}

// connection statistics
public class ConnectionStats
{
    public int TotalConnections { get; set; }
    public int WorkersOnline { get; set; }
    public int SupervisorsOnline { get; set; }
    public int AdminsOnline { get; set; }
}

// online worker information
public class OnlineWorker
{
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime ConnectedAt { get; set; }
    public DateTime? LastActivity { get; set; }
    public double? LastLatitude { get; set; }
    public double? LastLongitude { get; set; }
}

#endregion
