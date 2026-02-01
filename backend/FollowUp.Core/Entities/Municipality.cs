namespace FollowUp.Core.Entities;

/// <summary>
/// Represents a municipality that uses the FollowUp system.
/// Each municipality has its own zones, users, and configuration.
/// </summary>
public class Municipality
{
    public int MunicipalityId { get; set; }

    /// <summary>
    /// Unique code identifier for the municipality (e.g., "ALBIREH", "RAMALLAH")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Arabic name of the municipality
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// English name of the municipality
    /// </summary>
    public string? NameEnglish { get; set; }

    /// <summary>
    /// Country where the municipality is located
    /// </summary>
    public string Country { get; set; } = "Palestine";

    /// <summary>
    /// Region/Governorate within the country
    /// </summary>
    public string? Region { get; set; }

    // Contact information
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }

    /// <summary>
    /// URL to the municipality's logo
    /// </summary>
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

    /// <summary>
    /// Maximum acceptable GPS accuracy in meters
    /// </summary>
    public double MaxAcceptableAccuracyMeters { get; set; } = 150.0;

    /// <summary>
    /// Whether the municipality is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// License expiration date (null = no expiration)
    /// </summary>
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

    /// <summary>
    /// Checks if a coordinate is within the municipality's bounding box
    /// </summary>
    public bool IsWithinBounds(double latitude, double longitude)
    {
        return latitude >= MinLatitude && latitude <= MaxLatitude &&
               longitude >= MinLongitude && longitude <= MaxLongitude;
    }
}
