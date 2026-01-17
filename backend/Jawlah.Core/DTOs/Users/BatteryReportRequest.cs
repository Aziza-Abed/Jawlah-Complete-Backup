using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Users;

public class BatteryReportRequest
{
    [Required]
    [Range(0, 100)]
    public int BatteryLevel { get; set; }
}
