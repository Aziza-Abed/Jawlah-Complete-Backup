using Jawlah.Core.Enums;

namespace Jawlah.Core.DTOs.Attendance;

public class AttendanceResponse
{
    public int AttendanceId { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int? ZoneId { get; set; }
    public string? ZoneName { get; set; }
    public DateTime CheckInEventTime { get; set; }
    public DateTime? CheckOutEventTime { get; set; }
    public double CheckInLatitude { get; set; }
    public double CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public TimeSpan? WorkDuration { get; set; }
    public bool IsValidated { get; set; }
    public string? ValidationMessage { get; set; }
    public AttendanceStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
