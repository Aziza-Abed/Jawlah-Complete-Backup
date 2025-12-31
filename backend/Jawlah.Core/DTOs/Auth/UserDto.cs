namespace Jawlah.Core.DTOs.Auth;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? WorkerType { get; set; }
    public string? Pin { get; set; } // 4-digit PIN for workers
    public string EmployeeId { get; set; } = string.Empty; // Employee ID (same as PIN for workers) - REQUIRED
    public string PhoneNumber { get; set; } = string.Empty; // REQUIRED - Mobile expects this
    public DateTime CreatedAt { get; set; }
}
