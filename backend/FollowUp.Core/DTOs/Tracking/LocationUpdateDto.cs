using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tracking;

public class LocationUpdateDto
{
    [Required(ErrorMessage = "latitude is required")]
    [Range(-90, 90, ErrorMessage = "latitude must be between -90 and 90")]
    public double Latitude { get; set; }

    [Required(ErrorMessage = "longitude is required")]
    [Range(-180, 180, ErrorMessage = "longitude must be between -180 and 180")]
    public double Longitude { get; set; }

    [Range(0, 1000, ErrorMessage = "speed must be between 0 and 1000 m/s")]
    public double? Speed { get; set; }

    [Range(0, 5000, ErrorMessage = "accuracy must be between 0 and 5000 meters")]
    public double? Accuracy { get; set; }

    [Range(0, 359.99, ErrorMessage = "heading must be between 0 and 359.99")]
    public double? Heading { get; set; }

    [Required(ErrorMessage = "timestamp is required")]
    public DateTime Timestamp { get; set; }
}
