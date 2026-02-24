namespace FollowUp.Core.Enums;

public enum AppealType
{
    // appeal against auto-rejected task completion
    TaskRejection = 1,

    // appeal against failed attendance check-in (outside zone)
    AttendanceFailure = 2
}
