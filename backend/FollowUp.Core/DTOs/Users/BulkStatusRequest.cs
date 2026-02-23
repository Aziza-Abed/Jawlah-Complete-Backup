using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

/// <summary>
/// UC16: Bulk enable/disable users request
/// Admin can enable/disable multiple users at once
/// </summary>
public class BulkStatusRequest
{
    [Required]
    [MinLength(1)]
    public List<int> UserIds { get; set; } = new();

    [Required]
    public UserStatus Status { get; set; }
}
