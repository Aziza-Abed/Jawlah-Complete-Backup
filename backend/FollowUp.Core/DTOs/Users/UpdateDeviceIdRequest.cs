using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

// request to update a worker's registered device ID
public class UpdateDeviceIdRequest
{
    [Required(ErrorMessage = "معرف الجهاز مطلوب")]
    [StringLength(100, ErrorMessage = "معرف الجهاز طويل جداً")]
    public string DeviceId { get; set; } = string.Empty;
}
