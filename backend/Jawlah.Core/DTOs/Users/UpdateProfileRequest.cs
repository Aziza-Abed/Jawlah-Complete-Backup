using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Users;

public class UpdateProfileRequest
{
    [EmailAddress]
    [StringLength(100)]
    public string? Email { get; set; }

    [Required]
    [StringLength(20)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string FullName { get; set; } = string.Empty;
}
