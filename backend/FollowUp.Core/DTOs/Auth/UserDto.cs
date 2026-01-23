namespace FollowUp.Core.DTOs.Auth;

public class UserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? WorkerType { get; set; }
    public string EmployeeId { get; set; } = string.Empty; // Employee ID - REQUIRED
    public string PhoneNumber { get; set; } = string.Empty; // REQUIRED - Mobile expects this
    public DateTime CreatedAt { get; set; }

    // Municipality information
    public int MunicipalityId { get; set; }
    public string MunicipalityCode { get; set; } = string.Empty;
    public string MunicipalityName { get; set; } = string.Empty;
    public string? MunicipalityNameEnglish { get; set; }
}
