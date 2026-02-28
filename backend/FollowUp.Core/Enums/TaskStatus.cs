namespace FollowUp.Core.Enums;

// Backend lifecycle: Pending → InProgress → UnderReview → Completed/Rejected
// Mobile-only states: Created, Submitted, Synced, FailedSync (local Hive storage only)
public enum TaskStatus
{
    Pending = 0,       // default state for new tasks
    InProgress = 1,
    UnderReview = 2,   // worker submitted evidence, awaiting supervisor review
    Completed = 3,     // supervisor approved
    Rejected = 4,      // supervisor rejected
    Created = 5,       // task just created, not yet assigned
    Assigned = 6,      // assigned to worker, not yet accepted
    Accepted = 7,      // worker accepted the task
    Submitted = 8,     // worker submitted for review (alias for UnderReview)
    Synced = 9,        // completed and synced to server
    Cancelled = 10,    // task cancelled
    FailedSync = 11    // sync to server failed
}
