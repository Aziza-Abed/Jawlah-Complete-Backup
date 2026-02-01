using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FollowUp.Core.Constants;

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

    /// <summary>
    /// Validates GPS coordinates for zero values and geographic boundaries
    /// </summary>
    /// <param name="latitude">The latitude coordinate to validate</param>
    /// <param name="longitude">The longitude coordinate to validate</param>
    /// <param name="allowZero">Whether to allow zero coordinates (default: false)</param>
    /// <returns>BadRequest with error message if invalid, null if valid</returns>
    protected IActionResult? ValidateGpsCoordinates(double latitude, double longitude, bool allowZero = false)
    {
        // Check for zero coordinates
        if (!allowZero && latitude == 0 && longitude == 0)
        {
            return BadRequest(new { message = "Invalid GPS coordinates. Please enable location services." });
        }

        // Check if coordinates are within valid geographic boundaries (Palestine region)
        if (!GeofencingConstants.IsWithinPalestine(latitude, longitude))
        {
            return BadRequest(new
            {
                message = "GPS coordinates are outside the valid service area.",
                latitude,
                longitude
            });
        }

        return null; // Coordinates are valid
    }
}
