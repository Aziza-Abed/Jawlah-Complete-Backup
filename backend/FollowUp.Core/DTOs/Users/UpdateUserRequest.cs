using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

/// <summary>
/// Request DTO for updating user information (Admin only)
/// Supports partial updates - only non-null fields are updated
/// </summary>
public class UpdateUserRequest
{
    /// <summary>
    /// New supervisor ID for worker transfer. Set to 0 or null to remove supervisor.
    /// Only applicable for workers.
    /// </summary>
    public int? SupervisorId { get; set; }

    /// <summary>
    /// Update user's full name
    /// </summary>
    public string? FullName { get; set; }

    /// <summary>
    /// Update user's phone number
    /// </summary>
    public string? PhoneNumber { get; set; }

    /// <summary>
    /// Update user's status (Active/Inactive)
    /// </summary>
    public UserStatus? Status { get; set; }
}
