using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace Jawlah.API.LiveTracking;

public interface ITrackingClient
{
    Task ReceiveLocationUpdate(int userId, double latitude, double longitude);
    Task ReceiveUserStatus(int userId, string status);
}

[Authorize]
public class TrackingHub : Hub<ITrackingClient>
{
    private readonly ILogger<TrackingHub> _logger;

    public TrackingHub(ILogger<TrackingHub> logger)
    {
        _logger = logger;
    }

    public async Task SendLocationUpdate(double latitude, double longitude)
    {
        var userIdStr = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (int.TryParse(userIdStr, out var userId))
        {
            // Broadcast to Supervisors group or all (for now All, targeting later)
            // In a real app, supervisors would join a "Supervisors" group
            await Clients.Group("Supervisors").ReceiveLocationUpdate(userId, latitude, longitude);
            
            _logger.LogTrace("User {UserId} updated location: {Lat}, {Lng}", userId, latitude, longitude);
        }
    }
    
    public async Task JoinSupervisorsGroup()
    {
        var role = Context.User?.FindFirst(ClaimTypes.Role)?.Value;
        if (role == "Admin" || role == "Supervisor")
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, "Supervisors");
        }
    }
}
