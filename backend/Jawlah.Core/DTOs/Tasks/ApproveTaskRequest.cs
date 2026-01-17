using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Tasks;

public class ApproveTaskRequest
{
    [StringLength(1000, ErrorMessage = "comments cannot exceed 1000 characters")]
    public string? Comments { get; set; } // Optional supervisor comments
}
