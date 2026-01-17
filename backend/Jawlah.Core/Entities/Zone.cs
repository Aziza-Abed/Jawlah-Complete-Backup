using NetTopologySuite.Geometries;

namespace Jawlah.Core.Entities;

public class Zone
{
    public int ZoneId { get; set; }

    // Municipality that this zone belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    public string ZoneName { get; set; } = string.Empty;
    public string ZoneCode { get; set; } = string.Empty;
    public string? Description { get; set; }
    public Geometry? Boundary { get; set; }
    public string? BoundaryGeoJson { get; set; }
    public double CenterLatitude { get; set; }
    public double CenterLongitude { get; set; }
    public double AreaSquareMeters { get; set; }
    public string? District { get; set; }
    public int Version { get; set; }
    public DateTime VersionDate { get; set; }
    public string? VersionNotes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public ICollection<UserZone> AssignedUsers { get; set; } = new List<UserZone>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
}
