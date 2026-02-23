using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Auth;

public class RegisterFcmTokenRequest
{
    [Required(ErrorMessage = "FCM token is required")]
    [StringLength(500, ErrorMessage = "FCM token is too long")]
    public string FcmToken { get; set; } = string.Empty;
}
