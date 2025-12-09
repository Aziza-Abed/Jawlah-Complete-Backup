namespace Jawlah.Core.DTOs.Zones;

public class ValidateLocationResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public ZoneResponse? Zone { get; set; }
}
