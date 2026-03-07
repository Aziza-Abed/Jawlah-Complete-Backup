using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

// password reset by admin
public class AdminResetPasswordRequest
{
    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
    public string NewPassword { get; set; } = string.Empty;
}
