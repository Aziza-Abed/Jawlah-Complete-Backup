using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Users;

public class ChangePasswordRequest
{
    [Required(ErrorMessage = "old password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "old password must be between 8 and 100 characters")]
    public string OldPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "new password is required")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "new password must be between 8 and 100 characters")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "confirm password is required")]
    [Compare("NewPassword", ErrorMessage = "passwords do not match")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
