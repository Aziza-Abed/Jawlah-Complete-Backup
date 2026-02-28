using System.Security.Claims;
using FollowUp.Core.Constants;
using FollowUp.Core.DTOs.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FollowUp.API.Controllers;

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

    // normalize pagination parameters to safe values
    protected (int Page, int PageSize) NormalizePagination(int page, int pageSize, int maxPageSize = 100, int defaultPageSize = 50)
    {
        if (pageSize < 1 || pageSize > maxPageSize) pageSize = defaultPageSize;
        if (page < 1) page = 1;
        return (page, pageSize);
    }

    // validate GPS coordinates are non-zero and within service area
    protected IActionResult? ValidateGpsCoordinates(double latitude, double longitude, bool allowZero = false)
    {
        if (!allowZero && latitude == 0 && longitude == 0)
            return BadRequest(ApiResponse<object>.ErrorResponse("إحداثيات GPS غير صالحة. يرجى تفعيل خدمات الموقع"));

        if (!GeofencingConstants.IsWithinPalestine(latitude, longitude))
            return BadRequest(ApiResponse<object>.ErrorResponse("إحداثيات GPS خارج منطقة الخدمة"));

        return null;
    }
}
