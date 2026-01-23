namespace FollowUp.Core.Entities;

/// <summary>
/// Represents a department within a municipality.
/// Examples: Health Department, Public Works Department, Agriculture Department
/// </summary>
public class Department
{
    public int DepartmentId { get; set; }

    /// <summary>
    /// Municipality that this department belongs to
    /// </summary>
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    /// <summary>
    /// Arabic name of the department
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// English name of the department
    /// </summary>
    public string? NameEnglish { get; set; }

    /// <summary>
    /// Unique code for the department (e.g., "HEALTH", "WORKS", "AGRI")
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Description of department responsibilities
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether the department is currently active
    /// </summary>
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
