using FollowUp.Core.Enums;

namespace FollowUp.Core.Entities;

public class User
{
    public int UserId { get; set; }

    // Municipality that this user belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    // Supervisor assignment for workers (null for supervisors/admins)
    public int? SupervisorId { get; set; }
    public User? Supervisor { get; set; }
    public ICollection<User> SupervisedWorkers { get; set; } = new List<User>();

    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public WorkerType? WorkerType { get; set; }

    // Department assignment (nullable for admins who may not belong to a department)
    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    // Team assignment for workers who work in teams (nullable for individual workers)
    public int? TeamId { get; set; }
    public Team? Team { get; set; }

    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? FcmToken { get; set; }

    // Login attempt tracking (SR1.5)
    public int FailedLoginAttempts { get; set; } = 0;
    public DateTime? LockoutEndTime { get; set; }

    // Device binding for security - stores the first registered device ID
    // If a different device tries to login with the same PIN, it will be rejected
    public string? RegisteredDeviceId { get; set; }

    // Work schedule for lateness/overtime tracking
    public TimeSpan ExpectedStartTime { get; set; } = new TimeSpan(8, 0, 0); // 08:00
    public TimeSpan ExpectedEndTime { get; set; } = new TimeSpan(16, 0, 0); // 16:00
    public int GraceMinutes { get; set; } = 15;

    // Warning system for policy violations (location mismatch, etc.)
    public int WarningCount { get; set; } = 0;
    public DateTime? LastWarningAt { get; set; }
    public string? LastWarningReason { get; set; }

    // Battery monitoring for field workers
    public int? LastBatteryLevel { get; set; }
    public DateTime? LastBatteryReportTime { get; set; }
    public bool IsLowBattery { get; set; } = false;

    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
    public ICollection<Issue> ReportedIssues { get; set; } = new List<Issue>();
    public ICollection<UserZone> AssignedZones { get; set; } = new List<UserZone>();
    public ICollection<Appeal> Appeals { get; set; } = new List<Appeal>();
}
