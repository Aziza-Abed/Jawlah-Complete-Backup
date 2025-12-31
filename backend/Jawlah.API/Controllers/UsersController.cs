using System.Security.Claims;
using AutoMapper;
using Jawlah.API;
using Jawlah.Core.DTOs.Common;
using Jawlah.Core.DTOs.Users;
using Jawlah.Core.Enums;
using Jawlah.Core.Interfaces.Repositories;
using Jawlah.Core.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[Route("api/[controller]")]
public class UsersController : BaseApiController
{
    private readonly IUserRepository _users;
    private readonly IAuthService _auth;
    private readonly ILogger<UsersController> _logger;
    private readonly IMapper _mapper;

    public UsersController(IUserRepository users, IAuthService auth, ILogger<UsersController> logger, IMapper mapper)
    {
        _users = users;
        _auth = auth;
        _logger = logger;
        _mapper = mapper;
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetAllUsers([FromQuery] UserStatus? status = null)
    {
        // get all users from the database
        var users = await _users.GetAllAsync();

        // filter by status if the supervisor wants only active or inactive users
        if (status.HasValue)
        {
            users = users.Where(u => u.Status == status.Value);
        }

        // return the list
        return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(
            users.Select(u => _mapper.Map<UserResponse>(u))));
    }

    [HttpGet("{id}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUserById(int id)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
    }

    [HttpGet("by-role/{role}")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetUsersByRole(UserRole role)
    {
        var users = await _users.GetByRoleAsync(role);

        return Ok(ApiResponse<IEnumerable<UserResponse>>.SuccessResponse(
            users.Select(u => _mapper.Map<UserResponse>(u))));
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        // find the current user
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        // update the fields with new values
        user.Email = request.Email;
        user.PhoneNumber = request.PhoneNumber;
        user.FullName = request.FullName;

        // save changes
        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        var userId = GetCurrentUserId();
        if (!userId.HasValue)
            return Unauthorized();

        var user = await _users.GetByIdAsync(userId.Value);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("المستخدم غير موجود"));

        if (!_auth.VerifyPassword(request.OldPassword, user.PasswordHash))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("رقم التعريف أو كلمة المرور غير صحيحة"));
        }

        if (!Utils.InputSanitizer.IsStrongPassword(request.NewPassword))
        {
            return BadRequest(ApiResponse<object>.ErrorResponse("كلمة المرور يجب أن تكون 8 أحرف على الأقل"));
        }

        user.PasswordHash = _auth.HashPassword(request.NewPassword);

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("User {UserId} changed password", userId);

        return Ok(ApiResponse<object?>.SuccessResponse(null, "Password changed successfully"));
    }

    [HttpPut("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateUserStatusRequest request)
    {
        var user = await _users.GetByIdAsync(id);
        if (user == null)
            return NotFound(ApiResponse<object>.ErrorResponse("User not found"));

        user.Status = request.Status;

        await _users.UpdateAsync(user);
        await _users.SaveChangesAsync();

        _logger.LogInformation("User {UserId} status updated to {Status}", id, request.Status);

        return Ok(ApiResponse<UserResponse>.SuccessResponse(_mapper.Map<UserResponse>(user)));
    }

    [HttpGet("active-workers-count")]
    [Authorize(Roles = "Admin,Supervisor")]
    public async Task<IActionResult> GetActiveWorkersCount()
    {
        var workers = await _users.GetByRoleAsync(UserRole.Worker);
        var activeCount = workers.Count(w => w.Status == UserStatus.Active);

        return Ok(ApiResponse<int>.SuccessResponse(activeCount));
    }
}
