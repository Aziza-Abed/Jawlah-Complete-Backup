using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

// request for GPS-based login (mobile workers) using Username + Password + DeviceID
// login is pure authentication, GPS is optional and not used for attendance
// attendance is handled automatically via geofencing
public class LoginWithGPSRequest
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    [StringLength(50, ErrorMessage = "اسم المستخدم طويل جداً")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور مطلوبة")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "كلمة المرور مطلوبة")]
    public string Password { get; set; } = string.Empty;

    // GPS fields are optional - kept for future use but not required for login
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double? Longitude { get; set; }

    [Range(0, 1000, ErrorMessage = "Accuracy must be between 0 and 1000 meters")]
    public double? Accuracy { get; set; }

    // Device ID for device binding security (2FA)
    [Required(ErrorMessage = "معرف الجهاز مطلوب")]
    [StringLength(100, ErrorMessage = "معرف الجهاز طويل جداً")]
    public string DeviceId { get; set; } = string.Empty;
}
