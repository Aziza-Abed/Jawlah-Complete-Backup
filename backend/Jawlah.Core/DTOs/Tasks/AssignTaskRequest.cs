using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Tasks;

public class AssignTaskRequest
{
    [Required(ErrorMessage = "User ID is required")]
    public int UserId { get; set; }
}
