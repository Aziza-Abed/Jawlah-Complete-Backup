using FollowUp.Core.Constants;

namespace FollowUp.API.Utils;

// input sanitization helper
public static class InputSanitizer
{
    // sanitize user input - trim and truncate
    // Note: XSS protection is handled by React (frontend) and JSON serialization (API)
    public static string SanitizeString(string? input, int maxLength = 500)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var sanitized = input.Trim();

        if (sanitized.Length > maxLength)
            sanitized = sanitized[..maxLength];

        return sanitized;
    }

    public static bool IsStrongPassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password)) return false;

        // Minimum password length from central config
        if (password.Length < AppConstants.MinPasswordLength) return false;

        // At least one letter
        if (!password.Any(char.IsLetter)) return false;

        // At least one digit
        if (!password.Any(char.IsDigit)) return false;

        // At least one special character
        if (!password.Any(c => !char.IsLetterOrDigit(c))) return false;

        return true;
    }
}
