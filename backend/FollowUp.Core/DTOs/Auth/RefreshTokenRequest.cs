using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

public class RefreshTokenRequest
{
    [Required(ErrorMessage = "رمز التحديث مطلوب")]
    public string RefreshToken { get; set; } = string.Empty;
}
