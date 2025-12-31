using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Attendance;

public class CheckInRequest
{
    [Required]
    [Range(-90, 90, ErrorMessage = "Latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required]
    [Range(-180, 180, ErrorMessage = "Longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    // GPS accuracy in meters (optional but recommended for validation)
    [Range(0, 1000, ErrorMessage = "Accuracy must be between 0 and 1000 meters")]
    public double? Accuracy { get; set; }
}
