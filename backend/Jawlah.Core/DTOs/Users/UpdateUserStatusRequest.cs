using System.ComponentModel.DataAnnotations;
using Jawlah.Core.Enums;

namespace Jawlah.Core.DTOs.Users;

public class UpdateUserStatusRequest
{
    [Required]
    public UserStatus Status { get; set; }
}
