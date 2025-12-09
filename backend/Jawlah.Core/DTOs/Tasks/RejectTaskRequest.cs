using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Tasks;

public class RejectTaskRequest
{
    [Required(ErrorMessage = "Rejection reason is required")]
    [MinLength(10, ErrorMessage = "Rejection reason must be at least 10 characters")]
    public string Reason { get; set; } = string.Empty;
}
