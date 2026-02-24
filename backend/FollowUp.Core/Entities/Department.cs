namespace FollowUp.Core.Entities;

// department within a municipality (e.g. Health, Public Works, Agriculture)
public class Department
{
    public int DepartmentId { get; set; }

    // municipality that this department belongs to
    public int MunicipalityId { get; set; }
    public Municipality Municipality { get; set; } = null!;

    // arabic name
    public string Name { get; set; } = string.Empty;

    // english name
    public string? NameEnglish { get; set; }

    // unique code (e.g., "HEALTH", "WORKS", "AGRI")
    public string Code { get; set; } = string.Empty;

    // description of department responsibilities
    public string? Description { get; set; }

    // whether the department is currently active
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public ICollection<Team> Teams { get; set; } = new List<Team>();
    public ICollection<User> Users { get; set; } = new List<User>();
}
