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
    public DateTime LastActivity { get; set; }
    public double LastLatitude { get; set; }
    public double LastLongitude { get; set; }
    private readonly object _lock = new();

    public void UpdateLocation(double latitude, double longitude, DateTime timestamp)
    {
        lock (_lock)
        {
            LastLatitude = latitude;
            LastLongitude = longitude;
            LastActivity = timestamp;
        }
    }

    public void TouchActivity()
    {
        lock (_lock)
        {
            LastActivity = DateTime.UtcNow;
        }
    }
}

// real-time tracking hub for location updates between workers and supervisors
[Authorize]
public class TrackingHub : Hub<ITrackingClient>
{
    private readonly ILogger<TrackingHub> _logger;
    private readonly ILocationHistoryRepository _locationHistory;

    // thread-safe dictionary to track active connections (SignalR is concurrent)
    private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();

    // cleanup stale connections every 10 minutes
    private static DateTime _lastCleanup = DateTime.UtcNow;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan StaleConnectionTimeout = TimeSpan.FromMinutes(30);

    // max age for batch location timestamps (24 hours)
    private static readonly TimeSpan MaxTimestampAge = TimeSpan.FromHours(24);

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

            // periodically clean up stale connections
            CleanupStaleConnections();

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

        // update the connection info in our memory (thread-safe via lock inside UserConnection)
        if (_connections.TryGetValue(Context.ConnectionId, out var conn))
        {
            conn.UpdateLocation(latitude, longitude, timestamp);
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
            _logger.LogError(ex, "Failed to save location to database for user {UserId}. Location data may be lost.", userId);
            // Continue to broadcast even if DB save fails — supervisor still sees real-time position
        }

        // send the new location to all supervisors so they can see it on the map
        try
        {
            await Clients.Group("Supervisors").ReceiveLocationUpdate(
                userId.Value, userName, latitude, longitude, timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast location for {UserName} — supervisor may have disconnected", userName);
        }

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

        var now = DateTime.UtcNow;

        // sort once, filter invalid coordinates and reject future/too-old timestamps
        var validLocations = locations
            .Where(l => IsValidGpsCoordinate(l.Latitude, l.Longitude))
            .Where(l => l.Timestamp <= now && (now - l.Timestamp) <= MaxTimestampAge)
            .OrderBy(l => l.Timestamp)
            .ToList();

        if (!validLocations.Any())
            return;

        _logger.LogInformation("Received {Count} batch location updates from {UserName} ({Rejected} rejected)",
            validLocations.Count, userName, locations.Count - validLocations.Count);

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
            _logger.LogError(ex, "Failed to save batch locations to database for user {UserId}. {Count} locations may be lost.",
                userId, validLocations.Count);
        }

        // broadcast the latest location to supervisors (not all 500 — just the most recent)
        try
        {
            var latest = validLocations.Last();
            await Clients.Group("Supervisors").ReceiveLocationUpdate(
                userId.Value, userName, latest.Latitude, latest.Longitude, latest.Timestamp);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast batch location for {UserName}", userName);
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

        try
        {
            await Clients.Group("Supervisors").ReceiveActivity(
                userId.Value, userName, activityType, description);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast activity for {UserName}", userName);
        }

        _logger.LogInformation("Activity reported: {ActivityType} by {UserName}", activityType, userName);
    }

    // worker notifies when entering/exiting a zone
    public async Task NotifyZoneEvent(int zoneId, string zoneName, string eventType)
    {
        var (userId, userName, _) = GetCurrentHubUser();

        if (!userId.HasValue)
            return;

        // broadcast to supervisors and zone workers
        try
        {
            await Clients.Group("Supervisors").ReceiveZoneEvent(
                userId.Value, userName, zoneId, zoneName, eventType);

            // also broadcast to other workers in the same zone (for coordination)
            await Clients.Group($"Zone_{zoneId}").ReceiveZoneEvent(
                userId.Value, userName, zoneId, zoneName, eventType);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to broadcast zone event for {UserName}", userName);
        }

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

        if (zoneId <= 0)
            return;

        if (role == "Worker")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
            _logger.LogInformation("Worker joined Zone group: {ZoneId}", zoneId);
        }
    }

    // worker leaves a zone-specific group
    public async Task LeaveZoneGroup(int zoneId)
    {
        var (_, _, role) = GetCurrentHubUser();

        if (zoneId <= 0)
            return;

        if (role == "Worker")
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");
            _logger.LogInformation("Worker left Zone group: {ZoneId}", zoneId);
        }
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
        // just update when we last heard from the client (thread-safe via lock inside UserConnection)
        if (_connections.TryGetValue(Context.ConnectionId, out var conn))
        {
            conn.TouchActivity();
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

    // Remove connections that haven't had any activity (no heartbeat, no location update)
    private void CleanupStaleConnections()
    {
        var now = DateTime.UtcNow;
        if (now - _lastCleanup < CleanupInterval)
            return;

        _lastCleanup = now;

        var staleKeys = _connections
            .Where(kvp => (now - kvp.Value.LastActivity) > StaleConnectionTimeout)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in staleKeys)
        {
            if (_connections.TryRemove(key, out var stale))
            {
                _logger.LogInformation("Cleaned up stale connection for {UserName} (inactive {Minutes}min)",
                    stale.UserName, (int)(now - stale.LastActivity).TotalMinutes);
            }
        }

        if (staleKeys.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} stale connections", staleKeys.Count);
        }
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
    public DateTime LastActivity { get; set; }
    public double LastLatitude { get; set; }
    public double LastLongitude { get; set; }
}

#endregion
