namespace Jawlah.Core.DTOs.Auth;

public class LoginResponse
{
    public bool Success { get; set; }
    public string? Token { get; set; }
    public string? RefreshToken { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public UserDto? User { get; set; }
    public string? Error { get; set; }
}

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? WorkerType { get; set; }
    public string? Pin { get; set; } // 4-digit PIN for workers
    public string? EmployeeId { get; set; } // Employee ID (same as PIN for workers)
    public string? PhoneNumber { get; set; }
    public DateTime CreatedAt { get; set; }
}
