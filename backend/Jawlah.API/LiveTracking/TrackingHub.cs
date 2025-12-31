using Jawlah.Core.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Collections.Concurrent;
using System.Security.Claims;

namespace Jawlah.API.LiveTracking;

// client-side methods that can be called from the server
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

// enhanced real-time tracking hub with connection management and zone-based broadcasting
[Authorize]
public class TrackingHub : Hub<ITrackingClient>
{
    private readonly ILogger<TrackingHub> _logger;
    private readonly IUserRepository _userRepository;

    // thread-safe dictionary to track active connections
    private static readonly ConcurrentDictionary<string, UserConnection> _connections = new();

    public TrackingHub(ILogger<TrackingHub> logger, IUserRepository userRepository)
    {
        _logger = logger;
        _userRepository = userRepository;
    }

    #region Connection Lifecycle

    // called when a client connects to the hub
    public override async Task OnConnectedAsync()
    {
        // get the user info from the connection
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;
        
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (userId.HasValue)
        {
            // create a connection object and save it in our list
            var connection = new UserConnection
            {
                UserId = userId.Value,
                UserName = userName ?? "Unknown",
                Role = role ?? "Unknown",
                ConnectionId = Context.ConnectionId,
                ConnectedAt = DateTime.UtcNow,
                LastActivity = DateTime.UtcNow
            };

            _connections.TryAdd(Context.ConnectionId, connection);

            // put the user in groups based on their role
            if (role == "Admin" || role == "Supervisor")
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
                    userId.Value,
                    userName ?? "Unknown",
                    "online");
            }

            // update the connection stats for everyone
            await BroadcastConnectionStats();

            _logger.LogInformation("User connected: " + userName);
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

            _logger.LogInformation("User disconnected: " + connection.UserName);
        }

        await base.OnDisconnectedAsync(exception);
    }

    #endregion

    #region Location Tracking

    // worker sends real-time location update
    public async Task SendLocationUpdate(double latitude, double longitude)
    {
        // get the user info
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!userId.HasValue)
        {
            _logger.LogWarning("Location update received from unauthenticated user");
            return;
        }

        // update the connection info in our memory
        if (_connections.TryGetValue(Context.ConnectionId, out var connection))
        {
            connection.LastLatitude = latitude;
            connection.LastLongitude = longitude;
            connection.LastActivity = DateTime.UtcNow;
        }

        // send the new location to all supervisors so they can see it on the map
        await Clients.Group("Supervisors").ReceiveLocationUpdate(
            userId.Value,
            userName ?? "Unknown",
            latitude,
            longitude,
            DateTime.UtcNow);

        _logger.LogDebug("Location updated for: " + userName);
    }

    // worker sends batch location updates (for offline sync)
    public async Task SendLocationBatch(List<LocationUpdate> locations)
    {
        // get user info
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!userId.HasValue || locations == null || !locations.Any())
        {
            return;
        }

        _logger.LogInformation("Received batch location updates from: " + userName);

        // broadcast each location to supervisors
        foreach (var location in locations.OrderBy(l => l.Timestamp))
        {
            await Clients.Group("Supervisors").ReceiveLocationUpdate(
                userId.Value,
                userName ?? "Unknown",
                location.Latitude,
                location.Longitude,
                location.Timestamp);
        }
    }

    #endregion

    #region Activity Notifications

    // worker notifies supervisors of an activity (task started, completed, etc.)
    public async Task NotifyActivity(string activityType, string description)
    {
        // get user info
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!userId.HasValue)
        {
            return;
        }

        // tell supervisors about the activity
        await Clients.Group("Supervisors").ReceiveActivity(
            userId.Value,
            userName ?? "Unknown",
            activityType,
            description);

        _logger.LogInformation("Activity reported: " + activityType + " by " + userName);
    }

    // worker notifies when entering/exiting a zone
    public async Task NotifyZoneEvent(int zoneId, string zoneName, string eventType)
    {
        // get user info
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        int? userId = int.TryParse(userIdStr, out var u) ? u : null;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value;

        if (!userId.HasValue)
        {
            return;
        }

        // broadcast to supervisors
        await Clients.Group("Supervisors").ReceiveZoneEvent(
            userId.Value,
            userName ?? "Unknown",
            zoneId,
            zoneName,
            eventType);

        // also broadcast to other workers in the same zone (for coordination)
        await Clients.Group($"Zone_{zoneId}").ReceiveZoneEvent(
            userId.Value,
            userName ?? "Unknown",
            zoneId,
            zoneName,
            eventType);

        _logger.LogInformation("Zone event: " + eventType + " at " + zoneName);
    }

    #endregion

    #region Group Management

    // supervisor explicitly joins supervisors group to receive worker updates
    public async Task JoinSupervisorsGroup()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Admin" || role == "Supervisor")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Supervisors");

            _logger.LogInformation("Supervisor joined tracking group");

            // send current online workers list
            SendOnlineWorkersList();
        }
    }

    // worker joins a zone-specific group for zone-based notifications
    public async Task JoinZoneGroup(int zoneId)
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (role == "Worker")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");

            _logger.LogInformation("Worker joined Zone group: " + zoneId);
        }
    }

    // worker leaves a zone-specific group
    public async Task LeaveZoneGroup(int zoneId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Zone_{zoneId}");

        _logger.LogInformation("Worker left Zone group: " + zoneId);
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
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;

        if (role != "Admin" && role != "Supervisor")
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
        if (_connections.TryGetValue(Context.ConnectionId, out var connection))
        {
            connection.LastActivity = DateTime.UtcNow;
        }

        return Task.CompletedTask;
    }

    #endregion

    #region Private Helper Methods

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

    private void SendOnlineWorkersList()
    {
        var onlineWorkers = GetOnlineWorkers();
        // this list exists for supervisors to call GetOnlineWorkers() manually
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
