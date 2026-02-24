namespace FollowUp.Core.DTOs.Teams;

// team data for API responses
public class TeamDto
{
    public int TeamId { get; set; }
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? TeamLeaderId { get; set; }
    public string? TeamLeaderName { get; set; }
    public int MaxMembers { get; set; }
    public int MembersCount { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
