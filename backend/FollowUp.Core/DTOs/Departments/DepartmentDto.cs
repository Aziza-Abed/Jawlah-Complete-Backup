namespace FollowUp.Core.DTOs.Departments;

public class DepartmentDto
{
    public int DepartmentId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public int UsersCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateDepartmentRequest
{
    public string Name { get; set; } = string.Empty;
    public string? NameEnglish { get; set; }
    public string Code { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class UpdateDepartmentRequest
{
    public string? Name { get; set; }
    public string? NameEnglish { get; set; }
    public string? Code { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
}
