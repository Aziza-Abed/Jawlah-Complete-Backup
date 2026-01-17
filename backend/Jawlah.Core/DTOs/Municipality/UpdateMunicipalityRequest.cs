using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Municipality;

/// <summary>
/// Request DTO for updating an existing municipality
/// </summary>
public class UpdateMunicipalityRequest
{
    [StringLength(200, MinimumLength = 2, ErrorMessage = "اسم البلدية يجب أن يكون بين 2 و 200 حرف")]
    public string? Name { get; set; }

    [StringLength(200)]
    public string? NameEnglish { get; set; }

    [StringLength(100)]
    public string? Region { get; set; }

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    [StringLength(500)]
    public string? LogoUrl { get; set; }

    // Bounding box
    [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90")]
    public double? MinLatitude { get; set; }

    [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90")]
    public double? MaxLatitude { get; set; }

    [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180")]
    public double? MinLongitude { get; set; }

    [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180")]
    public double? MaxLongitude { get; set; }

    // Work schedule defaults
    public string? DefaultStartTime { get; set; }  // Format: "HH:mm:ss"
    public string? DefaultEndTime { get; set; }    // Format: "HH:mm:ss"

    [Range(0, 60, ErrorMessage = "وقت السماح يجب أن يكون بين 0 و 60 دقيقة")]
    public int? DefaultGraceMinutes { get; set; }

    [Range(10, 500, ErrorMessage = "دقة GPS يجب أن تكون بين 10 و 500 متر")]
    public double? MaxAcceptableAccuracyMeters { get; set; }

    public bool? IsActive { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
}
