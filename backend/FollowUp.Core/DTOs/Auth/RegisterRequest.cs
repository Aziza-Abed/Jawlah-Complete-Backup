using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Username is required")]
    [StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Full name is required")]
    [StringLength(100, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "Phone number is required")]
    [RegularExpression(@"^[\d\s\-\+\(\)]{7,20}$", ErrorMessage = "Invalid phone number format")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Role is required")]
    public UserRole Role { get; set; }

    public WorkerType? WorkerType { get; set; }

    [StringLength(4, MinimumLength = 4, ErrorMessage = "PIN must be exactly 4 digits")]
    [RegularExpression(@"^\d{4}$", ErrorMessage = "PIN must be exactly 4 digits")]
    public string? Pin { get; set; } // Optional - legacy field, no longer used

    // Department ID for the user (optional)
    public int? DepartmentId { get; set; }

    // Team ID for workers who work in teams (optional)
    public int? TeamId { get; set; }

    // Supervisor ID for workers (required for workers, ignored for supervisors/admins)
    public int? SupervisorId { get; set; }

    // Municipality ID for the user
    public int? MunicipalityId { get; set; }

    // Department name (text description, not ID)
    public string? Department { get; set; }
}
