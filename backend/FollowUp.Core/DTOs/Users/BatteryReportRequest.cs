using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Users;

public class BatteryReportRequest
{
    [Required]
    [Range(0, 100)]
    public int BatteryLevel { get; set; }

    public bool IsCharging { get; set; }

    public bool IsLowBattery { get; set; }

    public DateTime? Timestamp { get; set; }
}
