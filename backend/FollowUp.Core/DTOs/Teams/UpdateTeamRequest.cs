using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Teams;

/// <summary>
/// Request DTO for updating an existing team
/// </summary>
public class UpdateTeamRequest
{
    [Required(ErrorMessage = "اسم الفريق مطلوب")]
    [StringLength(100, ErrorMessage = "اسم الفريق يجب أن لا يتجاوز 100 حرف")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "رمز الفريق مطلوب")]
    [StringLength(20, ErrorMessage = "رمز الفريق يجب أن لا يتجاوز 20 حرف")]
    public string Code { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "الوصف يجب أن لا يتجاوز 500 حرف")]
    public string? Description { get; set; }

    public int? TeamLeaderId { get; set; }

    [Range(1, 100, ErrorMessage = "الحد الأقصى للأعضاء يجب أن يكون بين 1 و 100")]
    public int MaxMembers { get; set; }

    public bool IsActive { get; set; }
}
