using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

// bulk enable/disable users - admin can enable/disable multiple users at once
public class BulkStatusRequest
{
    [Required]
    [MinLength(1)]
    public List<int> UserIds { get; set; } = new();

    [Required]
    public UserStatus Status { get; set; }
}
