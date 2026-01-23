using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

/// <summary>
/// Request to update a worker's registered device ID
/// </summary>
public class UpdateDeviceIdRequest
{
    [Required(ErrorMessage = "معرف الجهاز مطلوب")]
    [StringLength(100, ErrorMessage = "معرف الجهاز طويل جداً")]
    public string DeviceId { get; set; } = string.Empty;
}
