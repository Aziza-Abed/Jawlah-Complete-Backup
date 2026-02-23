namespace FollowUp.Core.Enums;

// workflow: Pending → InProgress → UnderReview (worker submits) → Completed/Rejected (supervisor review)
public enum TaskStatus
{
    Pending = 0,
    InProgress = 1,
    UnderReview = 2,   // worker submitted evidence, awaiting supervisor review
    Completed = 3,     // supervisor approved
    Rejected = 4       // supervisor rejected
}
