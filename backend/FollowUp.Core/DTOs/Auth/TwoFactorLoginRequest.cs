namespace Jawlah.Core.DTOs.Auth;

public class TwoFactorLoginRequest
{
    public string Username { get; set; } = string.Empty;
    public string Pin { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string DeviceId { get; set; } = string.Empty;
}

public class TwoFactorVerifyRequest
{
    public string SessionToken { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}

public class TwoFactorLoginResponse
{
    public string SessionToken { get; set; } = string.Empty; // temporary token
    public DateTime ExpiresAt { get; set; }
    public string PhoneNumber { get; set; } = string.Empty; // masked: +970***1234
}
