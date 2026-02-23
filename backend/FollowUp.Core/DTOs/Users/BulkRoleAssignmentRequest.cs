using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

/// <summary>
/// SR22.5: Bulk role assignment request
/// Admin can assign roles to multiple users at once
/// </summary>
public class BulkRoleAssignmentRequest
{
    [Required]
    [MinLength(1)]
    public List<int> UserIds { get; set; } = new();

    [Required]
    public UserRole NewRole { get; set; }
}
