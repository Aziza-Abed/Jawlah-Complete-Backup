using FollowUp.Core.Enums;

namespace FollowUp.Core.Entities;

public class Attendance
{
    public int AttendanceId { get; set; }

    // Computed column for unique constraint (prevents duplicate check-ins per user per day)
    public DateTime CheckInDate { get; private set; }

    // Municipality that this attendance record belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    public int UserId { get; set; }
    public int? ZoneId { get; set; }
    public DateTime CheckInEventTime { get; set; }
    public DateTime? CheckInSyncTime { get; set; }
    public DateTime? CheckOutEventTime { get; set; }
    public DateTime? CheckOutSyncTime { get; set; }
    public double CheckInLatitude { get; set; }
    public double CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public bool IsValidated { get; set; }
    public string? ValidationMessage { get; set; }
    public TimeSpan? WorkDuration { get; set; }
    public AttendanceStatus Status { get; set; }
    public bool IsSynced { get; set; }
    public int SyncVersion { get; set; }

    // UR8: Manual attendance with supervisor approval
    public bool IsManual { get; set; } = false;
    public string? ManualReason { get; set; }
    public int? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public User? ApprovedByUser { get; set; }

    // Lateness and overtime tracking
    public int LateMinutes { get; set; } = 0;
    public int EarlyLeaveMinutes { get; set; } = 0;
    public int OvertimeMinutes { get; set; } = 0;
    public string AttendanceType { get; set; } = "OnTime"; // OnTime, Late, EarlyLeave, Overtime

    // Approval status for manual/GPS-failed entries
    public string ApprovalStatus { get; set; } = "AutoApproved"; // AutoApproved, Pending, Approved, Rejected

    public User User { get; set; } = null!;
    public Zone? Zone { get; set; }
}
