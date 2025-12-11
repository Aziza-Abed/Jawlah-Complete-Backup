using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Users;

public class ChangePasswordRequest
{
    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string OldPassword { get; set; } = string.Empty;

    [Required]
    [StringLength(100, MinimumLength = 6)]
    public string NewPassword { get; set; } = string.Empty;

    [Required]
    [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
