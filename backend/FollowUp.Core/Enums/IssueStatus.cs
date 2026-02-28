namespace FollowUp.Core.Enums;

public enum IssueStatus
{
    New = 0,                // newly reported
    Forwarded = 1,          // forwarded to a municipal department
    Resolved = 2,           // issue resolved/closed
    InProgress = 3,         // supervisor is reviewing (not yet converted)
    Closed = 4,             // closed without action
    ConvertedToTask = 5     // supervisor created a task from this issue
}
