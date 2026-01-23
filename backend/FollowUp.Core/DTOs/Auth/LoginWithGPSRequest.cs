using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

// request for GPS-based login (auto check-in) using Username + Password + GPS + DeviceID
public class LoginWithGPSRequest
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [StringLength(50, ErrorMessage = "اسم المستخدم طويل جداً")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, ErrorMessage = "كلمة المرور طويلة جداً")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    // GPS accuracy in meters (optional but recommended for validation)
    [Range(0, 1000, ErrorMessage = "Accuracy must be between 0 and 1000 meters")]
    public double? Accuracy { get; set; }

    // Device ID for device binding security (2FA)
    // On first login, the device is registered. Subsequent logins must be from the same device.
    [Required(ErrorMessage = "معرف الجهاز مطلوب")]
    [StringLength(100, ErrorMessage = "معرف الجهاز طويل جداً")]
    public string DeviceId { get; set; } = string.Empty;
}
