namespace FollowUp.Core.DTOs.Reports;

// Simple aggregated data - frontend handles presentation

public class TasksReportData
{
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
    public int Cancelled { get; set; }
    public int ActiveWorkers { get; set; }
    public int TotalWorkers { get; set; }
    public List<TasksByPeriod> ByPeriod { get; set; } = new();
    public List<TaskItem> Tasks { get; set; } = new();
}

public class TasksByPeriod
{
    public string Label { get; set; } = "";
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
}

public class TaskItem
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Worker { get; set; } = "";
    public string Zone { get; set; } = "";
    public string Status { get; set; } = "";
    public string Priority { get; set; } = "";
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ProgressPercentage { get; set; }
    public bool IsAutoRejected { get; set; }
}

public class WorkersReportData
{
    public int TotalWorkers { get; set; }
    public int CheckedIn { get; set; }
    public int Absent { get; set; }
    public int CompliancePercent { get; set; }
    public List<AttendanceByPeriod> ByPeriod { get; set; } = new();
    public List<WorkerWorkload> TopWorkload { get; set; } = new();
    public List<WorkerItem> Workers { get; set; } = new();
}

public class AttendanceByPeriod
{
    public string Label { get; set; } = "";
    public int Present { get; set; }
    public int Absent { get; set; }
}

public class WorkerWorkload
{
    public int UserId { get; set; }
    public string Name { get; set; } = "";
    public int ActiveTasks { get; set; }
}

public class WorkerItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public bool IsPresent { get; set; }
    public DateTime? LastCheckIn { get; set; }
    public int ActiveTasks { get; set; }
    public int CompletedTasks { get; set; }
}

public class ZonesReportData
{
    public int TotalZones { get; set; }
    public int TotalTasks { get; set; }
    public int TotalCompleted { get; set; }
    public int TotalInProgress { get; set; }
    public int TotalDelayed { get; set; }
    public string HighestPressureZone { get; set; } = "";
    public List<ZoneItem> Zones { get; set; } = new();
}

public class ZoneItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Total { get; set; }
    public int Completed { get; set; }
    public int InProgress { get; set; }
    public int Delayed { get; set; }
    public DateTime? LastUpdate { get; set; }
}

// ========== INDIVIDUAL WORKER REPORT ==========

public class IndividualWorkerReportData
{
    public int WorkerId { get; set; }
    public string WorkerName { get; set; } = "";
    public string? WorkerType { get; set; }
    public string? Department { get; set; }
    public string Period { get; set; } = "";
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }

    public AttendanceSummary Attendance { get; set; } = new();
    public TaskSummary Tasks { get; set; } = new();
    public WarningSummary Warnings { get; set; } = new();
    public List<TaskItem> RecentTasks { get; set; } = new();
    public List<AttendanceItem> RecentAttendance { get; set; } = new();
}

public class AttendanceSummary
{
    public int TotalWorkDays { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int LateDays { get; set; }
    public double AttendancePercentage { get; set; }
    public int TotalOvertimeMinutes { get; set; }
    public double AverageWorkHours { get; set; }
}

public class TaskSummary
{
    public int TotalAssigned { get; set; }
    public int Completed { get; set; }
    public int Approved { get; set; }
    public int Rejected { get; set; }
    public int AutoRejected { get; set; }
    public int InProgress { get; set; }
    public int Pending { get; set; }
    public double CompletionRate { get; set; }
    public int LocationWarnings { get; set; }
}

public class WarningSummary
{
    public int TotalWarnings { get; set; }
    public DateTime? LastWarningDate { get; set; }
    public string? LastWarningReason { get; set; }
}

public class AttendanceItem
{
    public DateTime Date { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime? CheckOut { get; set; }
    public string? WorkDuration { get; set; }
    public bool IsLate { get; set; }
    public int? LatenessMinutes { get; set; }
    public int? OvertimeMinutes { get; set; }
    public string? Zone { get; set; }
}

public class SupervisorStatsItem
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string Status { get; set; } = "Active";
    public SupervisorStatsDetail Stats { get; set; } = new();
}

public class SupervisorStatsDetail
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int CompletionRate { get; set; }
    public int OpenIssues { get; set; }
    public int AttendanceRate { get; set; }
}

// ========== ADMIN SUPERVISOR MONITORING ==========

/// <summary>
/// Enhanced supervisor monitoring data for Admin dashboard
/// </summary>
public class AdminSupervisorMonitoringData
{
    public List<SupervisorMonitoringItem> Supervisors { get; set; } = new();
    public List<AdminAlert> Alerts { get; set; } = new();
    public AdminSummary Summary { get; set; } = new();
}

public class SupervisorMonitoringItem
{
    public int UserId { get; set; }
    public string FullName { get; set; } = "";
    public string Username { get; set; } = "";
    public string? PhoneNumber { get; set; }
    public string Status { get; set; } = "Active";
    public DateTime? LastLoginAt { get; set; }

    // Workers under this supervisor
    public int WorkersCount { get; set; }
    public int ActiveWorkersToday { get; set; }

    // Tasks created by this supervisor for their workers
    public int TasksAssignedThisMonth { get; set; }
    public int TasksCompletedThisMonth { get; set; }
    public int TasksPendingReview { get; set; }
    public int TasksDelayed { get; set; }
    public double CompletionRate { get; set; }

    // Response metrics
    public double AvgResponseTimeHours { get; set; } // Time to review completed tasks

    // Issues
    public int IssuesReportedByWorkers { get; set; }
    public int IssuesResolved { get; set; }
    public int IssuesPending { get; set; }

    // Performance indicator: Good, Warning, Critical
    public string PerformanceStatus { get; set; } = "Good";
}

public class AdminAlert
{
    public int Id { get; set; }
    public string Type { get; set; } = ""; // TooManyWorkers, PerformanceDrop, HighDelayRate, LowActivity
    public string Severity { get; set; } = "Warning"; // Info, Warning, Critical
    public string Message { get; set; } = "";
    public int? SupervisorId { get; set; }
    public string? SupervisorName { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AdminSummary
{
    public int TotalSupervisors { get; set; }
    public int ActiveSupervisors { get; set; }
    public int TotalWorkers { get; set; }
    public int ActiveWorkersToday { get; set; }
    public int TotalTasksThisMonth { get; set; }
    public int CompletedTasksThisMonth { get; set; }
    public double OverallCompletionRate { get; set; }
    public int TotalPendingIssues { get; set; }
}

