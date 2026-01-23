using System.ComponentModel.DataAnnotations;

namespace FollowUp.Core.DTOs.Tasks;

public class AssignTaskRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }
}
