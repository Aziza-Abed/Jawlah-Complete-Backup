using Jawlah.Core.DTOs.Attendance;

namespace Jawlah.Core.DTOs.Auth;

// response for GPS-based login with auto check-in
public class LoginWithGPSResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public AttendanceResponse? Attendance { get; set; }  // Auto-created attendance
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }
}
