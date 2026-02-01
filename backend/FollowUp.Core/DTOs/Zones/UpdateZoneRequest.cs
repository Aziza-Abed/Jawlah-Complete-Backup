namespace FollowUp.Core.DTOs.Zones;

public class UpdateZoneRequest
{
    public string? ZoneName { get; set; }

    public string? ZoneCode { get; set; }

    public string? Description { get; set; }

    public double? AreaSquareMeters { get; set; }

    public string? BoundaryGeoJson { get; set; }

    public string? District { get; set; }
}
