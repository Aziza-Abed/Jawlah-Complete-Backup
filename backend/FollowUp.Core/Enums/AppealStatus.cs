namespace FollowUp.Core.Enums;

/// <summary>
/// Status of an appeal
/// </summary>
public enum AppealStatus
{
    /// <summary>
    /// Appeal submitted, awaiting supervisor review
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Appeal approved by supervisor - task/attendance reinstated
    /// </summary>
    Approved = 2,

    /// <summary>
    /// Appeal rejected by supervisor - original rejection stands
    /// </summary>
    Rejected = 3
}
