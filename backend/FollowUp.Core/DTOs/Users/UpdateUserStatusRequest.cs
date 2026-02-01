using System.ComponentModel.DataAnnotations;
using FollowUp.Core.Enums;

namespace FollowUp.Core.DTOs.Users;

public class UpdateUserStatusRequest
{
    [Required]
    public UserStatus Status { get; set; }
}
