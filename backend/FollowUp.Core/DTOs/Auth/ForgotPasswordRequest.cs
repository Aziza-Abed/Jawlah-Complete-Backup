using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

public class ForgotPasswordRequest
{
    [Required(ErrorMessage = "اسم المستخدم مطلوب")]
    public string Username { get; set; } = string.Empty;
}
