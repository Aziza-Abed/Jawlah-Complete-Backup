-- ========================================================================
-- Refresh Today's Data: Attendance + Task Statuses
-- Run this anytime to simulate active workers with varied task states
-- ========================================================================
SET QUOTED_IDENTIFIER ON;
GO
USE FollowUpNew;
GO

DECLARE @Now DATETIME = GETUTCDATE();
DECLARE @Today DATE = CAST(@Now AS DATE);
DECLARE @MunicipalityId INT = 1;

-- ========================================================================
-- 1. ATTENDANCE: Delete stale "today" records, insert fresh ones
-- ========================================================================
DELETE FROM [Attendances] WHERE CAST(CheckInEventTime AS DATE) = @Today;

-- Get worker IDs (first 15 health workers + 5 works workers = 20 checked in)
DECLARE @W1 INT = (SELECT UserId FROM Users WHERE Username = 'worker1');
DECLARE @W2 INT = (SELECT UserId FROM Users WHERE Username = 'worker2');
DECLARE @W3 INT = (SELECT UserId FROM Users WHERE Username = 'worker3');

-- Get zone IDs
DECLARE @Z1 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE MunicipalityId = @MunicipalityId ORDER BY ZoneId);
DECLARE @Z2 INT = (SELECT ZoneId FROM Zones WHERE MunicipalityId = @MunicipalityId ORDER BY ZoneId OFFSET 1 ROWS FETCH NEXT 1 ROWS ONLY);

-- Check in worker1-3 (currently working, no checkout)
INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
VALUES
(@MunicipalityId, @W1, @Z1, DATEADD(MINUTE,5,DATEADD(HOUR,7,CAST(@Today AS DATETIME))), DATEADD(MINUTE,5,DATEADD(HOUR,7,CAST(@Today AS DATETIME))), NULL, NULL, 31.8961, 35.2081, 8.5, NULL, NULL, NULL, 1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime'),
(@MunicipalityId, @W2, @Z1, DATEADD(MINUTE,12,DATEADD(HOUR,7,CAST(@Today AS DATETIME))), DATEADD(MINUTE,12,DATEADD(HOUR,7,CAST(@Today AS DATETIME))), NULL, NULL, 31.8962, 35.2082, 12.0, NULL, NULL, NULL, 1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime'),
(@MunicipalityId, @W3, @Z2, DATEADD(MINUTE,8,DATEADD(HOUR,7,CAST(@Today AS DATETIME))), DATEADD(MINUTE,8,DATEADD(HOUR,7,CAST(@Today AS DATETIME))), NULL, NULL, 31.9071, 35.2121, 10.0, NULL, NULL, NULL, 1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime');

-- Check in workers 4-15 (health workers, currently working)
DECLARE @i INT = 4;
WHILE @i <= 15
BEGIN
    DECLARE @wId INT = (SELECT UserId FROM Users WHERE Username = 'worker' + CAST(@i AS VARCHAR(10)));
    IF @wId IS NOT NULL
    BEGIN
        DECLARE @zId INT = (SELECT TOP 1 ZoneId FROM Zones WHERE MunicipalityId = @MunicipalityId ORDER BY NEWID());
        DECLARE @minOffset INT = ABS(CHECKSUM(NEWID())) % 20;
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
        VALUES (@MunicipalityId, @wId, @zId,
            DATEADD(MINUTE, @minOffset, DATEADD(HOUR, 7, CAST(@Today AS DATETIME))),
            DATEADD(MINUTE, @minOffset, DATEADD(HOUR, 7, CAST(@Today AS DATETIME))),
            NULL, NULL,
            31.89 + (ABS(CHECKSUM(NEWID())) % 20) * 0.001, 35.20 + (ABS(CHECKSUM(NEWID())) % 20) * 0.001,
            CAST(5 + ABS(CHECKSUM(NEWID())) % 15 AS FLOAT),
            NULL, NULL, NULL,
            1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1,
            CASE WHEN @minOffset > 15 THEN @minOffset - 15 ELSE 0 END, 0, 0,
            CASE WHEN @minOffset > 15 THEN 'Late' ELSE 'OnTime' END);
    END
    SET @i = @i + 1;
END

-- Check in 5 works workers (101-105)
SET @i = 101;
WHILE @i <= 105
BEGIN
    SET @wId = (SELECT UserId FROM Users WHERE Username = 'worker' + CAST(@i AS VARCHAR(10)));
    IF @wId IS NOT NULL
    BEGIN
        SET @zId = (SELECT TOP 1 ZoneId FROM Zones WHERE MunicipalityId = @MunicipalityId ORDER BY NEWID());
        SET @minOffset = ABS(CHECKSUM(NEWID())) % 15;
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
        VALUES (@MunicipalityId, @wId, @zId,
            DATEADD(MINUTE, @minOffset, DATEADD(HOUR, 7, CAST(@Today AS DATETIME))),
            DATEADD(MINUTE, @minOffset, DATEADD(HOUR, 7, CAST(@Today AS DATETIME))),
            NULL, NULL,
            31.89 + (ABS(CHECKSUM(NEWID())) % 20) * 0.001, 35.20 + (ABS(CHECKSUM(NEWID())) % 20) * 0.001,
            CAST(5 + ABS(CHECKSUM(NEWID())) % 15 AS FLOAT),
            NULL, NULL, NULL,
            1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime');
    END
    SET @i = @i + 1;
END

PRINT 'Inserted ~20 attendance records for today (all currently checked in)';

-- ========================================================================
-- 2. TASKS: Update existing tasks to have varied statuses
-- ========================================================================
-- TaskStatus: Pending=0, InProgress=1, UnderReview=2, Completed=3

-- Set some tasks to InProgress (started today)
UPDATE Tasks SET Status = 1, StartedAt = DATEADD(HOUR, 8, CAST(@Today AS DATETIME))
WHERE AssignedToUserId = @W1 AND Status = 0
AND TaskId IN (SELECT TOP 2 TaskId FROM Tasks WHERE AssignedToUserId = @W1 AND Status = 0 ORDER BY TaskId);

UPDATE Tasks SET Status = 1, StartedAt = DATEADD(HOUR, 8, CAST(@Today AS DATETIME))
WHERE AssignedToUserId = @W2 AND Status = 0
AND TaskId IN (SELECT TOP 2 TaskId FROM Tasks WHERE AssignedToUserId = @W2 AND Status = 0 ORDER BY TaskId);

UPDATE Tasks SET Status = 1, StartedAt = DATEADD(HOUR, 8, CAST(@Today AS DATETIME))
WHERE AssignedToUserId = @W3 AND Status = 0
AND TaskId IN (SELECT TOP 1 TaskId FROM Tasks WHERE AssignedToUserId = @W3 AND Status = 0 ORDER BY TaskId);

-- Set some tasks to Completed (done today)
UPDATE Tasks SET Status = 3, StartedAt = DATEADD(HOUR, 7, CAST(@Today AS DATETIME)), CompletedAt = DATEADD(HOUR, 10, CAST(@Today AS DATETIME)), CompletionNotes = N'تم الإنجاز بنجاح', ProgressPercentage = 100
WHERE AssignedToUserId = @W1 AND Status = 0
AND TaskId IN (SELECT TOP 1 TaskId FROM Tasks WHERE AssignedToUserId = @W1 AND Status = 0 ORDER BY TaskId);

UPDATE Tasks SET Status = 3, StartedAt = DATEADD(HOUR, 7, CAST(@Today AS DATETIME)), CompletedAt = DATEADD(HOUR, 11, CAST(@Today AS DATETIME)), CompletionNotes = N'تم التنفيذ', ProgressPercentage = 100
WHERE AssignedToUserId = @W2 AND Status = 0
AND TaskId IN (SELECT TOP 1 TaskId FROM Tasks WHERE AssignedToUserId = @W2 AND Status = 0 ORDER BY TaskId);

UPDATE Tasks SET Status = 3, StartedAt = DATEADD(HOUR, 8, CAST(@Today AS DATETIME)), CompletedAt = DATEADD(HOUR, 12, CAST(@Today AS DATETIME)), CompletionNotes = N'تم بنجاح', ProgressPercentage = 100
WHERE AssignedToUserId = @W3 AND Status = 0
AND TaskId IN (SELECT TOP 2 TaskId FROM Tasks WHERE AssignedToUserId = @W3 AND Status = 0 ORDER BY TaskId);

-- Set some tasks to UnderReview (submitted by worker, awaiting supervisor)
UPDATE Tasks SET Status = 2, StartedAt = DATEADD(HOUR, 8, CAST(@Today AS DATETIME)), ProgressPercentage = 100, ProgressNotes = N'بانتظار مراجعة المشرف'
WHERE AssignedToUserId = @W1 AND Status = 0
AND TaskId IN (SELECT TOP 1 TaskId FROM Tasks WHERE AssignedToUserId = @W1 AND Status = 0 ORDER BY TaskId);

UPDATE Tasks SET Status = 2, StartedAt = DATEADD(HOUR, 9, CAST(@Today AS DATETIME)), ProgressPercentage = 100, ProgressNotes = N'بانتظار مراجعة المشرف'
WHERE AssignedToUserId = @W3 AND Status = 0
AND TaskId IN (SELECT TOP 1 TaskId FROM Tasks WHERE AssignedToUserId = @W3 AND Status = 0 ORDER BY TaskId);

-- Also update some tasks for workers 4-10 to have variety
DECLARE @j INT = 4;
WHILE @j <= 10
BEGIN
    DECLARE @workerId INT = (SELECT UserId FROM Users WHERE Username = 'worker' + CAST(@j AS VARCHAR(10)));
    IF @workerId IS NOT NULL
    BEGIN
        -- 2 InProgress
        UPDATE Tasks SET Status = 1, StartedAt = DATEADD(HOUR, 7 + (@j % 3), CAST(@Today AS DATETIME))
        WHERE AssignedToUserId = @workerId AND Status = 0
        AND TaskId IN (SELECT TOP 2 TaskId FROM Tasks WHERE AssignedToUserId = @workerId AND Status = 0 ORDER BY TaskId);

        -- 1 Completed
        UPDATE Tasks SET Status = 3, StartedAt = DATEADD(HOUR, 7, CAST(@Today AS DATETIME)), CompletedAt = DATEADD(HOUR, 9 + (@j % 4), CAST(@Today AS DATETIME)), CompletionNotes = N'تم الإنجاز', ProgressPercentage = 100
        WHERE AssignedToUserId = @workerId AND Status = 0
        AND TaskId IN (SELECT TOP 1 TaskId FROM Tasks WHERE AssignedToUserId = @workerId AND Status = 0 ORDER BY TaskId);
    END
    SET @j = @j + 1;
END

PRINT 'Updated tasks: mixed Pending/InProgress/Completed/UnderReview statuses';

-- Summary
SELECT 'Tasks' AS [Table],
    SUM(CASE WHEN Status = 0 THEN 1 ELSE 0 END) AS Pending,
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS InProgress,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS UnderReview,
    SUM(CASE WHEN Status = 3 THEN 1 ELSE 0 END) AS Completed
FROM Tasks;

SELECT 'Attendance Today' AS [Table],
    SUM(CASE WHEN Status = 1 THEN 1 ELSE 0 END) AS CheckedIn,
    SUM(CASE WHEN Status = 2 THEN 1 ELSE 0 END) AS CheckedOut
FROM Attendances WHERE CAST(CheckInEventTime AS DATE) = @Today;
