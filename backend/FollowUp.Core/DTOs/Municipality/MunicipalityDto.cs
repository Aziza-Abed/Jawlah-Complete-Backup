namespace FollowUp.Core.DTOs.Municipality;

/// <summary>
/// DTO for municipality data
/// </summary>
public class MunicipalityDto
{
    public int MunicipalityId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public string Country { get; set; } = "Palestine";
    public string? Region { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }
    public string? Address { get; set; }
    public string? LogoUrl { get; set; }

    // Bounding box
    public double MinLatitude { get; set; }
    public double MaxLatitude { get; set; }
    public double MinLongitude { get; set; }
    public double MaxLongitude { get; set; }

    // Work schedule defaults
    public string DefaultStartTime { get; set; } = "08:00:00";
    public string DefaultEndTime { get; set; } = "16:00:00";
    public int DefaultGraceMinutes { get; set; } = 15;
    public double MaxAcceptableAccuracyMeters { get; set; } = 150.0;

    public bool IsActive { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Statistics
    public int UsersCount { get; set; }
    public int ZonesCount { get; set; }
}
