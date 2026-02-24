using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

// request for updating user information (Admin only)
// supports partial updates - only non-null fields are updated
public class UpdateUserRequest
{
    // new supervisor ID for worker transfer, set to 0 or null to remove supervisor
    public int? SupervisorId { get; set; }

    // update user's department, set to 0 or null to remove department
    public int? DepartmentId { get; set; }

    // update user's full name
    public string? FullName { get; set; }

    // update user's phone number
    public string? PhoneNumber { get; set; }

    // update user's status (Active/Inactive)
    public UserStatus? Status { get; set; }
}
