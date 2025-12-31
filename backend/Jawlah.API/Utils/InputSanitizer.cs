using System.Net;
using System.Text.RegularExpressions;

namespace Jawlah.API.Utils;

// input sanitization helper for XSS and injection protection
public static class InputSanitizer
{
    // sanitize user input to prevent XSS attacks
    public static string SanitizeString(string? input, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // trim whitespace
        var sanitized = input.Trim();

        // html encode to prevent XSS (converts <, >, &, ", ' to safe entities)
        sanitized = WebUtility.HtmlEncode(sanitized);

        // remove weird characters
        sanitized = Regex.Replace(sanitized, @"[\x00-\x1F\x7F]", "");

        // if too long, cut it
        if (sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength];

        return sanitized;
    }

    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;
        
        // Minimum 8 characters
        if (password.Length < 8) return false;
        
        // At least one letter
        if (!password.Any(char.IsLetter)) return false;
        
        // At least one digit
        if (!password.Any(char.IsDigit)) return false;

        return true;
    }

    // validate PIN is 4 digits only
    public static bool IsValidPin(string? pin)
    {
        if (string.IsNullOrWhiteSpace(pin))
            return false;

        return Regex.IsMatch(pin, @"^\d{4}$");
    }

    // validate coordinates are in valid range
    public static bool IsValidLatitude(double lat)
    {
        return lat >= -90 && lat <= 90;
    }

    public static bool IsValidLongitude(double lon)
    {
        return lon >= -180 && lon <= 180;
    }
}
