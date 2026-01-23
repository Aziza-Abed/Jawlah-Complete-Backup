using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Attendance;

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

    // Lateness and overtime tracking
    public int LateMinutes { get; set; } = 0;
    public int EarlyLeaveMinutes { get; set; } = 0;
    public int OvertimeMinutes { get; set; } = 0;
    public string AttendanceType { get; set; } = "OnTime"; // OnTime, Late, EarlyLeave, Overtime

    // Manual/GPS failure handling
    public bool IsManual { get; set; } = false;
    public string? ManualReason { get; set; }
    public string ApprovalStatus { get; set; } = "AutoApproved"; // AutoApproved, Pending, Approved, Rejected
    public int? ApprovedByUserId { get; set; }
    public string? ApprovedByUserName { get; set; }
    public DateTime? ApprovedAt { get; set; }
}
