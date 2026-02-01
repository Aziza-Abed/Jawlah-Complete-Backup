using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Auth;

// request for PIN-based login with optional GPS for auto check-in
// If location is provided and valid, worker is automatically checked in
// If location is missing or invalid, worker must check-in manually later
public class LoginWithPinRequest
{
    [Required(ErrorMessage = "الرقم السري مطلوب")]
    [StringLength(4, ErrorMessage = "الرقم السري يجب أن يكون 4 أرقام", MinimumLength = 4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "الرقم السري يجب أن يكون 4 أرقام فقط")]
    public string Pin { get; set; } = string.Empty;

    // Device ID for device binding security (prevents PIN theft)
    // On first login, the device is registered. Subsequent logins must be from the same device.
    [StringLength(100, ErrorMessage = "Device ID too long")]
    public string? DeviceId { get; set; }

    // Optional location for auto check-in
    // If provided and valid, creates attendance record automatically
    [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90")]
    public double? Latitude { get; set; }

    [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180")]
    public double? Longitude { get; set; }

    // GPS accuracy in meters (for validation quality)
    public double? Accuracy { get; set; }

    // If true and GPS fails, allow manual check-in with reason
    public bool AllowManualCheckIn { get; set; } = false;

    // Reason for manual check-in when GPS is unavailable
    [StringLength(500, ErrorMessage = "السبب طويل جداً")]
    public string? ManualCheckInReason { get; set; }
}
