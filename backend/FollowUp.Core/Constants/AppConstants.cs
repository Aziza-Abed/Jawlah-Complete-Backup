namespace FollowUp.Core.Constants;

// application-wide constants for configuration values
public static class AppConstants
{
    #region Pagination

    // default page size for paginated endpoints
    public const int DefaultPageSize = 50;

    // maximum allowed page size
    public const int MaxPageSize = 100;

    // minimum allowed page size
    public const int MinPageSize = 1;

    #endregion

    #region Authentication & Security

    // maximum failed login attempts before account lockout
    public const int MaxFailedLoginAttempts = 5;

    // account lockout duration in minutes
    public const int LockoutMinutes = 15;

    // default JWT token expiration in minutes (24 hours)
    public const int DefaultJwtExpirationMinutes = 1440;

    // OTP code expiration in minutes
    public const int OtpExpirationMinutes = 5;

    // cooldown between OTP resend requests in seconds
    public const int OtpResendCooldownSeconds = 60;

    // maximum OTP verification attempts
    public const int MaxOtpAttempts = 3;

    #endregion

    #region Task Distance Validation

    // distance in meters beyond which task submission is auto-rejected
    public const int HardRejectDistanceMeters = 500;

    // distance in meters that triggers a warning but allows submission
    public const int WarningDistanceMeters = 100;

    #endregion

    #region Online Status

    // minutes since last location update to consider a worker online
    public const int OnlineThresholdMinutes = 15;

    #endregion

    #region Input Validation

    // default maximum length for sanitized text inputs
    public const int DefaultMaxInputLength = 500;

    // minimum username length
    public const int MinUsernameLength = 3;

    // minimum password length
    public const int MinPasswordLength = 8;

    // OTP code length
    public const int OtpCodeLength = 6;

    #endregion

    #region Battery Monitoring

    // maximum battery percentage change allowed in one minute (fraud detection)
    public const int MaxBatteryDeltaPerMinute = 50;

    #endregion

    #region Database

    // database command timeout in seconds
    public const int DbCommandTimeoutSeconds = 60;

    #endregion

    #region Reports

    // default number of items to include in report summaries
    public const int ReportItemLimit = 100;

    // performance threshold percentage below which workers are flagged
    public const int LowPerformanceThreshold = 50;

    // number of delayed tasks that triggers a performance alert
    public const int DelayedTasksAlertThreshold = 5;

    // minimum tasks assigned before performance can be evaluated
    public const int MinTasksForEvaluation = 10;

    #endregion
}
