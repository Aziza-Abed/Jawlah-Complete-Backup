namespace FollowUp.Core.Entities;

// municipality that uses the FollowUp system, each has its own zones, users, and configuration
public class Municipality
{
    public int MunicipalityId { get; set; }

    // unique code identifier (e.g., "ALBIREH", "RAMALLAH")
    public string Code { get; set; } = string.Empty;

    // arabic name
    public string Name { get; set; } = string.Empty;

    // english name
    public string? NameEnglish { get; set; }

    // country
    public string Country { get; set; } = "Palestine";

    // region/governorate
    public string? Region { get; set; }

    // Contact information
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }

    // URL to the municipality's logo
    public string? LogoUrl { get; set; }

    // Geographic bounding box for the municipality area
    // Used for quick validation before checking zones
    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }

    // Work schedule defaults for the municipality
    // Individual users can override these values
    public TimeSpan DefaultStartTime { get; set; } = new TimeSpan(8, 0, 0); // 08:00
    public TimeSpan DefaultEndTime { get; set; } = new TimeSpan(16, 0, 0); // 16:00
    public int DefaultGraceMinutes { get; set; } = 15;

    // maximum acceptable GPS accuracy in meters
    public double MaxAcceptableAccuracyMeters { get; set; } = 50.0;

    // whether the municipality is currently active
    public bool IsActive { get; set; } = true;

    // license expiration date (null = no expiration)
    public DateTime? LicenseExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Department> Departments { get; set; } = new List<Department>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Zone> Zones { get; set; } = new List<Zone>();
    public ICollection<Task> Tasks { get; set; } = new List<Task>();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public ICollection<Issue> Issues { get; set; } = new List<Issue>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();

    // checks if a coordinate is within the municipality's bounding box
    public bool IsWithinBounds(double latitude, double longitude)
    {
        return latitude >= MinLatitude && latitude <= MaxLatitude &&
               longitude >= MinLongitude && longitude <= MaxLongitude;
    }
}
