namespace FollowUp.Core.Entities;

// team within a department, allows shared task assignment where multiple workers collaborate
public class Team
{
    public int TeamId { get; set; }

    // department that this team belongs to
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    // team name
    public string Name { get; set; } = string.Empty;

    // unique code for the team (e.g., "WORKS-T1", "AGRI-T2")
    public string Code { get; set; } = string.Empty;

    // description of team responsibilities or area
    public string? Description { get; set; }

    // team leader user ID (optional - a worker can be designated as team leader)
    public int? TeamLeaderId { get; set; }
    public User? TeamLeader { get; set; }

    // maximum number of members allowed in this team
    public int MaxMembers { get; set; } = 10;

    // whether the team is currently active
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
}
