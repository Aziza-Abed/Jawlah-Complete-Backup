namespace Jawlah.Core.Enums;

// workflow: Pending → InProgress → Completed → Approved/Rejected (supervisor review)
// or: Pending → Cancelled
public enum TaskStatus
{
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Cancelled = 4,
    Approved = 5,    // supervisor approved completed task
    Rejected = 6     // supervisor rejected completed task
}
