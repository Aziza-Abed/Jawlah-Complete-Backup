using Jawlah.Core.Enums;

namespace Jawlah.Core.Entities;

public class User
{
    // common weak pins to reject for security
    private static readonly string[] WeakPins = new[]
    {
        "0000", "1111", "2222", "3333", "4444", "5555", "6666", "7777", "8888", "9999",
        "1234", "4321", "0123", "9876", "5678", "8765",
        "1212", "2121", "1010", "2020"
    };

    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string? Pin { get; set; }
    public string? Email { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public WorkerType? WorkerType { get; set; }
    public string? Department { get; set; }
    public UserStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public string? FcmToken { get; set; }
    public ICollection<Attendance> AttendanceRecords { get; set; } = new List<Attendance>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
    public ICollection<Issue> ReportedIssues { get; set; } = new List<Issue>();
    public ICollection<UserZone> AssignedZones { get; set; } = new List<UserZone>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    // set and validate worker pin
    public void SetPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
        {
            throw new ArgumentException("PIN cannot be empty");
        }

        if (pin.Length != 4 || !pin.All(char.IsDigit))
        {
            throw new ArgumentException("PIN must be exactly 4 digits");
        }

        if (WeakPins.Contains(pin))
        {
            throw new ArgumentException(
                "PIN is too common and insecure. Please choose a different PIN.");
        }

        Pin = pin;
    }

    // validate pin strength
    public static bool IsStrongPin(string pin)
    {
        if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4 || !pin.All(char.IsDigit))
        {
            return false;
        }

        return !WeakPins.Contains(pin);
    }
}
