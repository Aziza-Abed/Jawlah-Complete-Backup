namespace FollowUp.Core.Enums;

public enum AppealStatus
{
    // awaiting supervisor review
    Pending = 1,

    // approved - task/attendance reinstated
    Approved = 2,

    // rejected - original rejection stands
    Rejected = 3
}
