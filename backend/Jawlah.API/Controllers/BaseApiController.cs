using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jawlah.API.Controllers;

[ApiController]
[Authorize]
public abstract class BaseApiController : ControllerBase
{
    // get the ID of the current user
    protected int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId) ? userId : null;
    }

    // get the role of the current user
    protected string? GetCurrentUserRole()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value;
    }
}
