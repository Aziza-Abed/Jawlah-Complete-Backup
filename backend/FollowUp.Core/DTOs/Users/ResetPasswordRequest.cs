using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

// SR1.6: Password reset by admin
public class ResetPasswordRequest
{
    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
    public string NewPassword { get; set; } = string.Empty;
}
