namespace Jawlah.Core.DTOs.Tracking;

public class LocationUpdateDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
}
