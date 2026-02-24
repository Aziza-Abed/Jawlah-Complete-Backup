namespace FollowUp.Core.Enums;

public enum IssueStatus
{
    New = 0,           // newly reported (equivalent to Open)
    Forwarded = 1,     // forwarded to a department
    Resolved = 2,      // issue resolved
    InProgress = 3,    // actively being worked on
    Closed = 4         // closed after resolution
}
