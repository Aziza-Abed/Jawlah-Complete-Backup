namespace FollowUp.Core.DTOs.Zones;

public class ZoneResponse
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double AreaSquareMeters { get; set; }
    public string? District { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
