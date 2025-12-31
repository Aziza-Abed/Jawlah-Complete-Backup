using System.ComponentModel.DataAnnotations;

namespace Jawlah.Core.DTOs.Users;

public class UpdateProfileRequest
{
    [EmailAddress(ErrorMessage = "invalid email format")]
    [StringLength(100, ErrorMessage = "email cannot exceed 100 characters")]
    public string? Email { get; set; }

    [Required(ErrorMessage = "phone number is required")]
    [Phone(ErrorMessage = "invalid phone number format")]
    [StringLength(20, ErrorMessage = "phone number cannot exceed 20 characters")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "full name is required")]
    [StringLength(100, ErrorMessage = "full name cannot exceed 100 characters")]
    public string FullName { get; set; } = string.Empty;
}
