using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

// bulk role assignment - admin can assign roles to multiple users at once
public class BulkRoleAssignmentRequest
{
    [Required]
    [MinLength(1)]
    public List<int> UserIds { get; set; } = new();

    [Required]
    public UserRole NewRole { get; set; }
}
