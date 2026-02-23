namespace FollowUp.Core.DTOs.Auth;

// Response for GPS-based login (mobile workers)
// UC2: Login is pure authentication. No attendance data in response.
// Attendance is handled automatically via geofencing (UC4).
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
