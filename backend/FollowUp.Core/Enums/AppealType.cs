namespace FollowUp.Core.Enums;

/// <summary>
/// Type of appeal
/// </summary>
public enum AppealType
{
    /// <summary>
    /// Appeal against auto-rejected task completion
    /// </summary>
    TaskRejection = 1,

    /// <summary>
    /// Appeal against failed attendance check-in (outside zone)
    /// </summary>
    AttendanceFailure = 2
}
