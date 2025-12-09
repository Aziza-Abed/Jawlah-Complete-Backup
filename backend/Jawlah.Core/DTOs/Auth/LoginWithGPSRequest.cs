using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Auth;

/// <summary>
/// Request for GPS-based login (auto check-in) using 4-digit PIN
/// </summary>
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
}
