namespace FollowUp.Core.Enums;

// workflow: Pending → InProgress → Completed → Approved/Rejected (supervisor review)
// or: Pending → Cancelled
public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    Completed = 2,
    Cancelled = 3,
    Approved = 4,    // supervisor approved completed task
    Rejected = 5     // supervisor rejected completed task
}
