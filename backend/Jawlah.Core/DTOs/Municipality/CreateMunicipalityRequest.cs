using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Municipality;

/// <summary>
/// Request DTO for creating a new municipality
/// </summary>
public class CreateMunicipalityRequest
{
    [Required(ErrorMessage = "رمز البلدية مطلوب")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "رمز البلدية يجب أن يكون بين 2 و 50 حرف")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "اسم البلدية مطلوب")]
    [StringLength(200, MinimumLength = 2, ErrorMessage = "اسم البلدية يجب أن يكون بين 2 و 200 حرف")]
    public string Name { get; set; } = string.Empty;

    [StringLength(200)]
    public string? NameEnglish { get; set; }

    [Required(ErrorMessage = "الدولة مطلوبة")]
    [StringLength(100)]
    public string Country { get; set; } = "Palestine";

    [StringLength(100)]
    public string? Region { get; set; }

    [EmailAddress(ErrorMessage = "البريد الإلكتروني غير صالح")]
    [StringLength(100)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(500)]
    public string? Address { get; set; }

    // Bounding box - required for geo-validation
    [Required(ErrorMessage = "خط العرض الأدنى مطلوب")]
    [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90")]
    public double MinLatitude { get; set; }

    [Required(ErrorMessage = "خط العرض الأقصى مطلوب")]
    [Range(-90, 90, ErrorMessage = "خط العرض يجب أن يكون بين -90 و 90")]
    public double MaxLatitude { get; set; }

    [Required(ErrorMessage = "خط الطول الأدنى مطلوب")]
    [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180")]
    public double MinLongitude { get; set; }

    [Required(ErrorMessage = "خط الطول الأقصى مطلوب")]
    [Range(-180, 180, ErrorMessage = "خط الطول يجب أن يكون بين -180 و 180")]
    public double MaxLongitude { get; set; }

    // Work schedule defaults (optional - uses system defaults if not provided)
    public string? DefaultStartTime { get; set; }  // Format: "HH:mm:ss"
    public string? DefaultEndTime { get; set; }    // Format: "HH:mm:ss"

    [Range(0, 60, ErrorMessage = "وقت السماح يجب أن يكون بين 0 و 60 دقيقة")]
    public int DefaultGraceMinutes { get; set; } = 15;

    [Range(10, 500, ErrorMessage = "دقة GPS يجب أن تكون بين 10 و 500 متر")]
    public double MaxAcceptableAccuracyMeters { get; set; } = 150.0;

    public DateTime? LicenseExpiresAt { get; set; }
}
