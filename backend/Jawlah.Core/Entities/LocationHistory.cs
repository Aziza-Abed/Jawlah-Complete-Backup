using System;

namespace Jawlah.Core.Entities;

public class LocationHistory
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double? Speed { get; set; }
    public double? Accuracy { get; set; }
    public double? Heading { get; set; }
    public DateTime Timestamp { get; set; }
    public bool IsSync { get; set; }
    
    // navigation property
    public virtual User User { get; set; } = null!;
}
