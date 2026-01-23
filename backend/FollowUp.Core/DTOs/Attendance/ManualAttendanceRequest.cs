using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Attendance;

// UR8: Manual attendance request when GPS is not available
public class ManualAttendanceRequest
{
    public int? ZoneId { get; set; }

    [Required(ErrorMessage = "سبب الحضور اليدوي مطلوب")]
    [MinLength(5, ErrorMessage = "السبب يجب أن يكون 5 أحرف على الأقل")]
    public string Reason { get; set; } = string.Empty;

    public DateTime? CheckInTime { get; set; }
}
