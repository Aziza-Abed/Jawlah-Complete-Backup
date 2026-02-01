using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Zones;

public class CreateZoneRequest
{
    [Required(ErrorMessage = "اسم المنطقة مطلوب")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم المنطقة يجب أن يكون بين 2 و 100 حرف")]
    public string ZoneName { get; set; } = string.Empty;

    [Required(ErrorMessage = "كود المنطقة مطلوب")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "كود المنطقة يجب أن يكون بين 1 و 20 حرف")]
    public string ZoneCode { get; set; } = string.Empty;

    public string? Description { get; set; }

    public double AreaSquareMeters { get; set; }

    public string? BoundaryGeoJson { get; set; }

    public string? District { get; set; }
}
