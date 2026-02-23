using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Zones;

public class UpdateZoneRequest
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم المنطقة يجب أن يكون بين 2 و 100 حرف")]
    public string? ZoneName { get; set; }

    [StringLength(20, MinimumLength = 1, ErrorMessage = "كود المنطقة يجب أن يكون بين 1 و 20 حرف")]
    public string? ZoneCode { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public double? AreaSquareMeters { get; set; }

    public string? BoundaryGeoJson { get; set; }

    [StringLength(100)]
    public string? District { get; set; }
}
