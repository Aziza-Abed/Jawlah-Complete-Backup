namespace FollowUp.Core.Constants;

/// <summary>
/// Application-wide constants for configuration values
/// Centralizes magic numbers that were previously scattered throughout the codebase
/// </summary>
public static class AppConstants
{
    #region Pagination

    /// <summary>Default page size for paginated endpoints</summary>
    public const int DefaultPageSize = 50;

    /// <summary>Maximum allowed page size</summary>
    public const int MaxPageSize = 100;

    /// <summary>Minimum allowed page size</summary>
    public const int MinPageSize = 1;

    #endregion

    #region Authentication & Security

    /// <summary>Maximum failed login attempts before account lockout</summary>
    public const int MaxFailedLoginAttempts = 5;

    /// <summary>Account lockout duration in minutes</summary>
    public const int LockoutMinutes = 15;

    /// <summary>Default JWT token expiration in minutes (24 hours)</summary>
    public const int DefaultJwtExpirationMinutes = 1440;

    /// <summary>OTP code expiration in minutes</summary>
    public const int OtpExpirationMinutes = 5;

    /// <summary>Cooldown between OTP resend requests in seconds</summary>
    public const int OtpResendCooldownSeconds = 60;

    /// <summary>Maximum OTP verification attempts</summary>
    public const int MaxOtpAttempts = 3;

    #endregion

    #region Task Distance Validation

    /// <summary>Distance in meters beyond which task submission is auto-rejected</summary>
    public const int HardRejectDistanceMeters = 500;

    /// <summary>Distance in meters that triggers a warning but allows submission</summary>
    public const int WarningDistanceMeters = 100;

    #endregion

    #region Online Status

    /// <summary>Minutes since last location update to consider a worker online</summary>
    public const int OnlineThresholdMinutes = 15;

    #endregion

    #region Input Validation

    /// <summary>Default maximum length for sanitized text inputs</summary>
    public const int DefaultMaxInputLength = 500;

    /// <summary>Minimum username length</summary>
    public const int MinUsernameLength = 3;

    /// <summary>Minimum password length</summary>
    public const int MinPasswordLength = 6;

    /// <summary>OTP code length</summary>
    public const int OtpCodeLength = 6;

    #endregion

    #region Battery Monitoring

    /// <summary>Maximum battery percentage change allowed in one minute (fraud detection)</summary>
    public const int MaxBatteryDeltaPerMinute = 50;

    #endregion

    #region Database

    /// <summary>Database command timeout in seconds</summary>
    public const int DbCommandTimeoutSeconds = 60;

    #endregion

    #region Reports

    /// <summary>Default number of items to include in report summaries</summary>
    public const int ReportItemLimit = 100;

    /// <summary>Performance threshold percentage below which workers are flagged</summary>
    public const int LowPerformanceThreshold = 50;

    /// <summary>Number of delayed tasks that triggers a performance alert</summary>
    public const int DelayedTasksAlertThreshold = 5;

    /// <summary>Minimum tasks assigned before performance can be evaluated</summary>
    public const int MinTasksForEvaluation = 10;

    #endregion
}
