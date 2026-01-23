-- Quick diagnostic query to check worker1 account
SELECT
    UserId,
    Username,
    FullName,
    Role,
    Status,
    PasswordHash,
    FailedLoginAttempts,
    LockoutEndTime,
    RegisteredDeviceId
FROM Users
WHERE Username = 'worker1';

-- Check if ANY users exist
SELECT COUNT(*) AS TotalUsers FROM Users;

-- Show all usernames
SELECT Username, Role, Status FROM Users;
