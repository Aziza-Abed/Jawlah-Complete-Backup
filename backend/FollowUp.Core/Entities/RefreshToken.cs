namespace FollowUp.Core.Entities;

/// <summary>
/// Refresh Token for JWT token refresh flow
/// Required by ERD in Chapter 3 - Class Diagram
/// </summary>
public class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    /// <summary>
    /// The refresh token string (hashed for security)
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// When the refresh token expires
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// When the refresh token was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When the token was revoked (null if still valid)
    /// </summary>
    public DateTime? RevokedAt { get; set; }

    /// <summary>
    /// The token that replaced this one (for token rotation)
    /// </summary>
    public string? ReplacedByToken { get; set; }

    /// <summary>
    /// Device ID that the token was issued to
    /// </summary>
    public string? DeviceId { get; set; }

    /// <summary>
    /// IP address where the token was issued from
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Check if the token is expired
    /// </summary>
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    /// <summary>
    /// Check if the token is revoked
    /// </summary>
    public bool IsRevoked => RevokedAt != null;

    /// <summary>
    /// Check if the token is active (not expired and not revoked)
    /// </summary>
    public bool IsActive => !IsExpired && !IsRevoked;
}
