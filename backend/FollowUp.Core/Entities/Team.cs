namespace FollowUp.Core.Entities;

/// <summary>
/// Represents a team within a department.
/// Teams allow for shared task assignment where multiple workers collaborate.
/// Example: Public Works groups of 5 workers, Agriculture teams of 3-4 workers
/// </summary>
public class Team
{
    public int TeamId { get; set; }

    /// <summary>
    /// Department that this team belongs to
    /// </summary>
    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    /// <summary>
    /// Team name (e.g., "فريق النظافة 1", "فريق الصيانة أ")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Unique code for the team (e.g., "WORKS-T1", "AGRI-T2")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of team responsibilities or area
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Team leader user ID (optional - a worker can be designated as team leader)
    /// </summary>
    public int? TeamLeaderId { get; set; }
    public User? TeamLeader { get; set; }

    /// <summary>
    /// Maximum number of members allowed in this team
    /// </summary>
    public int MaxMembers { get; set; } = 10;

    /// <summary>
    /// Whether the team is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<Task> AssignedTasks { get; set; } = new List<Task>();
}
