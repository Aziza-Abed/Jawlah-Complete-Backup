using System.Security.Claims;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Users;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Jawlah.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserRepository _userRepo;
    private readonly JawlahDbContext _context;
    private readonly IAuthService _authService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserRepository userRepo, JawlahDbContext context, IAuthService authService, ILogger<UsersController> logger)
    {
        _userRepo = userRepo;
        _context = context;
        _authService = authService;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAllUsers([FromQuery] UserStatus? status = null)
    {
        var users = await _userRepo.GetAllAsync();

        if (status.HasValue)
        {
            users = users.Where(u => u.Status == status.Value);
        }

        return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(
            users.Select(u => new UserResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                FullName = u.FullName,
                Role = u.Role,
                WorkerType = u.WorkerType,
                Department = u.Department,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        return Ok(ApiResponse<UserResponse>.SuccessResponse(new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            Role = user.Role,
            WorkerType = user.WorkerType,
            Department = user.Department,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        }));
    }

    [HttpGet("by-role/{role}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUsersByRole(UserRole role)
    {
        var users = await _userRepo.GetByRoleAsync(role);

        return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(
            users.Select(u => new UserResponse
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                FullName = u.FullName,
                Role = u.Role,
                WorkerType = u.WorkerType,
                Department = u.Department,
                Status = u.Status,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt
            })));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.FullName = request.FullName;

        await _userRepo.UpdateAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated profile", userId);

        return Ok(ApiResponse<UserResponse>.SuccessResponse(new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            Role = user.Role,
            WorkerType = user.WorkerType,
            Department = user.Department,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        }));
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _userRepo.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        if (!_authService.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("Current password is incorrect"));
        }

        user.PasswordHash = _authService.HashPassword(request.NewPassword);

        await _userRepo.UpdateAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed password", userId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Password changed successfully"));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _userRepo.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        user.Status = request.Status;

        await _userRepo.UpdateAsync(user);
        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} status updated to {Status}", id, request.Status);

        return Ok(ApiResponse<UserResponse>.SuccessResponse(new UserResponse
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            PhoneNumber = user.PhoneNumber,
            FullName = user.FullName,
            Role = user.Role,
            WorkerType = user.WorkerType,
            Department = user.Department,
            Status = user.Status,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt
        }));
    }

    [HttpGet("active-workers-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetActiveWorkersCount()
    {
        var workers = await _userRepo.GetByRoleAsync(UserRole.Worker);
        var activeCount = workers.Count(w => w.Status == UserStatus.Active);

        return Ok(ApiResponse<int>.SuccessResponse(activeCount));
    }
}
