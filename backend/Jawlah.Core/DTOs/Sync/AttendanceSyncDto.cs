namespace Jawlah.Core.DTOs.Sync;

public class AttendanceSyncDto
{
    public string? ClientId { get; set; }  // Client-side temporary ID
    public int UserId { get; set; }
    public DateTime CheckInTime { get; set; }
    public DateTime? CheckOutTime { get; set; }
    public double CheckInLatitude { get; set; }
    public double CheckInLongitude { get; set; }
    public double? CheckOutLatitude { get; set; }
    public double? CheckOutLongitude { get; set; }
    public bool IsValidated { get; set; }
    public string? ValidationMessage { get; set; }
}
