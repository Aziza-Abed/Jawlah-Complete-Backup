namespace FollowUp.Core.Constants;

// app-wide constants
public static class AppConstants
{
    // pagination
    public const int DefaultPageSize = 50;
    public const int MaxPageSize = 100;
    public const int MinPageSize = 1;

    // auth & security
    public const int MaxFailedLoginAttempts = 5;
    public const int LockoutMinutes = 15;
    public const int DefaultJwtExpirationMinutes = 1440; // 24 hours
    public const int OtpExpirationMinutes = 5;
    public const int OtpResendCooldownSeconds = 60;
    public const int MaxOtpAttempts = 3;

    // task distance validation
    public const int HardRejectDistanceMeters = 500; // auto-reject beyond this
    public const int WarningDistanceMeters = 100;    // warning but allows submit

    // online status
    public const int OnlineThresholdMinutes = 15;

    // input validation
    public const int DefaultMaxInputLength = 500;
    public const int MinUsernameLength = 3;
    public const int MinPasswordLength = 8;
    public const int OtpCodeLength = 6;

    // battery monitoring
    public const int MaxBatteryDeltaPerMinute = 50; // fraud detection

    // database
    public const int DbCommandTimeoutSeconds = 60;
    public const int DbMaxRetryDelaySeconds = 5;
    public const int DbMaxRetryCount = 3;

    // business rules
    public const int MaxActiveTasksPerWorker = 5;

    // input length limits
    public const int TitleMaxLength = 200;
    public const int DescriptionMaxLength = 2000;
    public const int LocationMaxLength = 500;

    // background service
    public const int BackgroundServiceCheckIntervalMinutes = 15;
    public const int AttendanceAutoCloseHours = 14;
    public const int MaxWorkDurationHours = 23;

    // file upload
    public const long MaxImageFileSizeBytes = 5 * 1024 * 1024;   // 5MB
    public const long MaxGisFileSizeBytes = 10 * 1024 * 1024;     // 10MB
    public const long MaxUploadSizeBytes = 10 * 1024 * 1024;      // 10MB

    // rate limiting (per IP per minute)
    public const int AuthRateLimitRequestsPerMinute = 60;
    public const int UploadRateLimitRequestsPerMinute = 30;

    // GIS
    public const double MetersPerDegree = 111319.9;

    // reports
    public const int ReportItemLimit = 100;
    public const int LowPerformanceThreshold = 50;
    public const int DelayedTasksAlertThreshold = 5;
    public const int MinTasksForEvaluation = 10;
}
