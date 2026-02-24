namespace FollowUp.Core.DTOs.Attendance;

// supervisor approval/rejection of manual attendance
public class ApproveAttendanceRequest
{
    public bool Approved { get; set; }
    public string? RejectionReason { get; set; }
}
