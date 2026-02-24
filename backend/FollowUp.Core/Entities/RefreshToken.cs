namespace FollowUp.Core.Entities;

// refresh token for JWT token refresh flow
public class RefreshToken
{
    public int RefreshTokenId { get; set; }

    public int UserId { get; set; }
    public User User { get; set; } = null!;

    // the refresh token string (hashed for security)
    public string Token { get; set; } = string.Empty;

    // when the refresh token expires
    public DateTime ExpiresAt { get; set; }

    // when the refresh token was created
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // when the token was revoked (null if still valid)
    public DateTime? RevokedAt { get; set; }

    // the token that replaced this one (for token rotation)
    public string? ReplacedByToken { get; set; }

    // device ID that the token was issued to
    public string? DeviceId { get; set; }

    // IP address where the token was issued from
    public string? IpAddress { get; set; }

    // check if the token is expired
    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;

    // check if the token is revoked
    public bool IsRevoked => RevokedAt != null;

    // check if the token is active (not expired and not revoked)
    public bool IsActive => !IsExpired && !IsRevoked;
}
