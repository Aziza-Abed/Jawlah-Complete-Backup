using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

/// <summary>
/// Base controller for all API controllers in the Jawlah system.
/// Provides common functionality like user authentication helpers and AutoMapper access.
/// </summary>
[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>
    /// AutoMapper instance for entity-to-DTO mapping.
    /// Injected automatically for all derived controllers.
    /// </summary>
    protected IMapper Mapper => HttpContext.RequestServices.GetRequiredService<IMapper>();

    /// <summary>
    /// Gets the current authenticated user's ID from JWT claims.
    /// </summary>
    /// <returns>User ID if authenticated, null otherwise</returns>
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null ? int.Parse(userIdClaim.Value) : null;
    }

    /// <summary>
    /// Gets the current authenticated user's ID, throws UnauthorizedAccessException if not found.
    /// </summary>
    /// <returns>User ID</returns>
    /// <exception cref="UnauthorizedAccessException">When user is not authenticated</exception>
    protected int GetCurrentUserIdOrThrow()
    {
        return GetCurrentUserId() ?? throw new UnauthorizedAccessException("User not authenticated");
    }

    /// <summary>
    /// Gets the current authenticated user's role from JWT claims.
    /// </summary>
    /// <returns>Role name if present, null otherwise</returns>
    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }

    /// <summary>
    /// Gets the current authenticated user's username from JWT claims.
    /// </summary>
    /// <returns>Username if present, null otherwise</returns>
    protected string? GetCurrentUsername()
    {
        return User.Identity?.Name;
    }
}
