using FollowUp.Core.DTOs.Attendance;

namespace FollowUp.Core.DTOs.Auth;

// response for GPS-based login with auto check-in
public class LoginWithGPSResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public AttendanceResponse? Attendance { get; set; }  // Auto-created attendance
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }

    // Flat fields for mobile compatibility
    public bool IsCheckedIn => Attendance != null;
    public int? ActiveAttendanceId => Attendance?.AttendanceId;
    public string CheckInStatus { get; set; } = "NotAttempted"; // NotAttempted, Success, PendingApproval, Failed
    public string? CheckInFailureReason { get; set; }
    public bool RequiresApproval { get; set; } = false;
    public bool IsLate => Attendance?.LateMinutes > 0;
    public int LateMinutes => Attendance?.LateMinutes ?? 0;
    public string AttendanceType => Attendance?.AttendanceType ?? "OnTime";
}
