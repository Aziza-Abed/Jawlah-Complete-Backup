using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Auth;

// request for GPS-based login (auto check-in) using 4-digit PIN
public class LoginWithGPSRequest
{
    [Required(ErrorMessage = "الرقم السري مطلوب")]
    [StringLength(4, ErrorMessage = "الرقم السري يجب أن يكون 4 أرقام", MinimumLength = 4)]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "الرقم السري يجب أن يكون 4 أرقام فقط")]
    public string Pin { get; set; } = string.Empty;

    [Required(ErrorMessage = "Latitude is required")]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "Longitude is required")]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    // GPS accuracy in meters (optional but recommended for validation)
    [Range(0, 1000, ErrorMessage = "Accuracy must be between 0 and 1000 meters")]
    public double? Accuracy { get; set; }
}
