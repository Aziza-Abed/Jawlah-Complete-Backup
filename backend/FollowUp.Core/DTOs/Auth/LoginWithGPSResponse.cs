namespace FollowUp.Core.DTOs.Auth;

// response for GPS-based login (mobile workers)
// login is pure authentication, no attendance data in response
// attendance is handled automatically via geofencing
public class LoginWithGPSResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
