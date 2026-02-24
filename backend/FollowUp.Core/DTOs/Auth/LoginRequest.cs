using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, ErrorMessage = "Username must be between 3 and 50 characters", MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, ErrorMessage = "Password must be at least 4 characters", MinimumLength = 4)]
    public string Password { get; set; } = string.Empty;

    // device ID for device binding (sent from mobile app body)
    // falls back to X-Device-Id header if not provided in body
    public string? DeviceId { get; set; }
}
