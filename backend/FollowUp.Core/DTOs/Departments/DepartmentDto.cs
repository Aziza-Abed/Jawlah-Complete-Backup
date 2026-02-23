using System.ComponentModel.DataAnnotations;

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
    [Required(ErrorMessage = "اسم القسم مطلوب")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "اسم القسم يجب أن يكون بين 2 و 100 حرف")]
    public string Name { get; set; } = string.Empty;

    [StringLength(100)]
    public string? NameEnglish { get; set; }

    [Required(ErrorMessage = "رمز القسم مطلوب")]
    [StringLength(20, MinimumLength = 1, ErrorMessage = "رمز القسم يجب أن يكون بين 1 و 20 حرف")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }
}

public class UpdateDepartmentRequest
{
    [StringLength(100, MinimumLength = 2)]
    public string? Name { get; set; }

    [StringLength(100)]
    public string? NameEnglish { get; set; }

    [StringLength(20, MinimumLength = 1)]
    public string? Code { get; set; }

    [StringLength(500)]
    public string? Description { get; set; }

    public bool? IsActive { get; set; }
}
