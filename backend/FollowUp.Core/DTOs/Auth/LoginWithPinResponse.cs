using Jawlah.Core.DTOs.Attendance;

namespace Jawlah.Core.DTOs.Auth;

// response for PIN-based login with optional auto check-in
// If location was provided, includes check-in result and lateness info
public class LoginWithPinResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public bool IsCheckedIn { get; set; }  // indicates if worker has active attendance (new or existing)
    public int? ActiveAttendanceId { get; set; }  // if checked in, the attendance ID
    public string Message { get; set; } = string.Empty;
    public string? Error { get; set; }

    // Auto check-in result details
    public AttendanceResponse? Attendance { get; set; }  // full attendance details if checked in

    // Lateness tracking
    public bool IsLate { get; set; } = false;
    public int LateMinutes { get; set; } = 0;
    public string AttendanceType { get; set; } = "OnTime"; // OnTime, Late, Manual

    // Check-in status - helps mobile app decide what to show
    public string CheckInStatus { get; set; } = "NotAttempted"; // NotAttempted, Success, PendingApproval, Failed

    // If check-in failed, reason why
    public string? CheckInFailureReason { get; set; }

    // If manual check-in was used (requires supervisor approval)
    public bool RequiresApproval { get; set; } = false;
}
