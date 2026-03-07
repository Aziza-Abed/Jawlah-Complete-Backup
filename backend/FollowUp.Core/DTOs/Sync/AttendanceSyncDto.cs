using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Sync;

public class AttendanceSyncDto
{
    [StringLength(100)]
    public string? ClientId { get; set; }  // Client-side temporary ID
    public int UserId { get; set; }
    public DateTime CheckInEventTime { get; set; }  // Mobile sends checkInEventTime
    public DateTime? CheckOutEventTime { get; set; }  // Mobile sends checkOutEventTime

    [Range(-90, 90)]
    public double CheckInLatitude { get; set; }

    [Range(-180, 180)]
    public double CheckInLongitude { get; set; }

    [Range(0, 10000)]
    public double? CheckInAccuracyMeters { get; set; }

    [Range(-90, 90)]
    public double? CheckOutLatitude { get; set; }

    [Range(-180, 180)]
    public double? CheckOutLongitude { get; set; }

    [Range(0, 10000)]
    public double? CheckOutAccuracyMeters { get; set; }

    public bool IsValidated { get; set; }

    [StringLength(500)]
    public string? ValidationMessage { get; set; }
    public int SyncVersion { get; set; }
}
