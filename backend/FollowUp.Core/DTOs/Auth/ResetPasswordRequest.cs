using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

public class ResetPasswordRequest
{
    [Required(ErrorMessage = "رمز الجلسة مطلوب")]
    public string SessionToken { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز التحقق مطلوب")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "رمز التحقق يجب أن يكون 6 أرقام")]
    public string OtpCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة")]
    [MinLength(8, ErrorMessage = "كلمة المرور يجب أن تكون 8 أحرف على الأقل")]
    public string NewPassword { get; set; } = string.Empty;
}
