namespace Jawlah.Core.Entities;

public class UserZone
{
    public int UserId { get; set; }
    public int ZoneId { get; set; }
    public DateTime AssignedAt { get; set; }
    public int AssignedByUserId { get; set; }
    public bool IsActive { get; set; }
    public User User { get; set; } = null!;
    public Zone Zone { get; set; } = null!;
    public User AssignedByUser { get; set; } = null!;
}
