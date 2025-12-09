using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Auth;

public class RegisterFcmTokenRequest
{
    [Required(ErrorMessage = "FCM token is required")]
    public string FcmToken { get; set; } = string.Empty;
}
