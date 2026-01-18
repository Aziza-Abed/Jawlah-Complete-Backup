IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
CREATE TABLE [Users] (
    [UserId] int NOT NULL IDENTITY,
    [Username] nvarchar(50) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [Email] nvarchar(100) NULL,
    [PhoneNumber] nvarchar(20) NOT NULL,
    [FullName] nvarchar(100) NOT NULL,
    [Role] int NOT NULL,
    [WorkerType] int NULL,
    [Department] nvarchar(100) NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [LastLoginAt] datetime2 NULL,
    CONSTRAINT [PK_Users] PRIMARY KEY ([UserId])
);

CREATE TABLE [Zones] (
    [ZoneId] int NOT NULL IDENTITY,
    [ZoneName] nvarchar(200) NOT NULL,
    [ZoneCode] nvarchar(50) NOT NULL,
    [Description] nvarchar(1000) NULL,
    [Boundary] geography NULL,
    [BoundaryGeoJson] nvarchar(max) NULL,
    [CenterLatitude] float(18) NOT NULL,
    [CenterLongitude] float(18) NOT NULL,
    [AreaSquareMeters] float NOT NULL,
    [District] nvarchar(100) NULL,
    [Version] int NOT NULL,
    [VersionDate] datetime2 NOT NULL,
    [VersionNotes] nvarchar(500) NULL,
    [IsActive] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Zones] PRIMARY KEY ([ZoneId])
);

CREATE TABLE [AuditLogs] (
    [AuditLogId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Action] nvarchar(100) NOT NULL,
    [EntityType] nvarchar(100) NULL,
    [EntityId] int NULL,
    [OldValue] nvarchar(max) NULL,
    [NewValue] nvarchar(max) NULL,
    [Timestamp] datetime2 NOT NULL,
    [IpAddress] nvarchar(50) NULL,
    [UserAgent] nvarchar(500) NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
);

CREATE TABLE [Notifications] (
    [NotificationId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Title] nvarchar(200) NOT NULL,
    [Message] nvarchar(1000) NOT NULL,
    [Type] int NOT NULL,
    [IsRead] bit NOT NULL,
    [IsSent] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [SentAt] datetime2 NULL,
    [ReadAt] datetime2 NULL,
    [FcmToken] nvarchar(500) NULL,
    [FcmMessageId] nvarchar(200) NULL,
    [PayloadJson] nvarchar(2000) NULL,
    CONSTRAINT [PK_Notifications] PRIMARY KEY ([NotificationId]),
    CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);

CREATE TABLE [SyncLogs] (
    [SyncLogId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [EntityType] nvarchar(100) NOT NULL,
    [EntityId] int NOT NULL,
    [Action] int NOT NULL,
    [EventTime] datetime2 NOT NULL,
    [SyncTime] datetime2 NOT NULL,
    [HadConflict] bit NOT NULL,
    [ConflictResolution] nvarchar(50) NULL,
    [ConflictDetails] nvarchar(2000) NULL,
    [DeviceId] nvarchar(100) NULL,
    [AppVersion] nvarchar(20) NULL,
    CONSTRAINT [PK_SyncLogs] PRIMARY KEY ([SyncLogId]),
    CONSTRAINT [FK_SyncLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE TABLE [Attendances] (
    [AttendanceId] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [ZoneId] int NULL,
    [CheckInEventTime] datetime2 NOT NULL,
    [CheckInSyncTime] datetime2 NULL,
    [CheckOutEventTime] datetime2 NULL,
    [CheckOutSyncTime] datetime2 NULL,
    [CheckInLatitude] float(18) NOT NULL,
    [CheckInLongitude] float(18) NOT NULL,
    [CheckOutLatitude] float(18) NULL,
    [CheckOutLongitude] float(18) NULL,
    [IsValidated] bit NOT NULL,
    [ValidationMessage] nvarchar(500) NULL,
    [WorkDuration] time NULL,
    [Status] int NOT NULL,
    [DeviceId] nvarchar(100) NULL,
    [IsSynced] bit NOT NULL,
    [SyncVersion] int NOT NULL,
    CONSTRAINT [PK_Attendances] PRIMARY KEY ([AttendanceId]),
    CONSTRAINT [FK_Attendances_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Attendances_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [Zones] ([ZoneId]) ON DELETE SET NULL
);

CREATE TABLE [Issues] (
    [IssueId] int NOT NULL IDENTITY,
    [ReportedByUserId] int NOT NULL,
    [ZoneId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Type] int NOT NULL,
    [Severity] int NOT NULL,
    [Status] int NOT NULL,
    [Latitude] float(18) NOT NULL,
    [Longitude] float(18) NOT NULL,
    [LocationDescription] nvarchar(500) NULL,
    [PhotoUrl] nvarchar(500) NULL,
    [AdditionalPhotosJson] nvarchar(2000) NULL,
    [ReportedAt] datetime2 NOT NULL,
    [ResolvedAt] datetime2 NULL,
    [ResolutionNotes] nvarchar(2000) NULL,
    [ResolvedByUserId] int NULL,
    [EventTime] datetime2 NOT NULL,
    [SyncTime] datetime2 NULL,
    [IsSynced] bit NOT NULL,
    [SyncVersion] int NOT NULL,
    CONSTRAINT [PK_Issues] PRIMARY KEY ([IssueId]),
    CONSTRAINT [FK_Issues_Users_ReportedByUserId] FOREIGN KEY ([ReportedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Issues_Users_ResolvedByUserId] FOREIGN KEY ([ResolvedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Issues_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [Zones] ([ZoneId]) ON DELETE SET NULL
);

CREATE TABLE [Tasks] (
    [TaskId] int NOT NULL IDENTITY,
    [AssignedToUserId] int NOT NULL,
    [AssignedByUserId] int NULL,
    [ZoneId] int NULL,
    [Title] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NOT NULL,
    [Priority] int NOT NULL,
    [Status] int NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [DueDate] datetime2 NULL,
    [StartedAt] datetime2 NULL,
    [CompletedAt] datetime2 NULL,
    [Latitude] float(18) NULL,
    [Longitude] float(18) NULL,
    [LocationDescription] nvarchar(500) NULL,
    [CompletionNotes] nvarchar(2000) NULL,
    [PhotoUrl] nvarchar(500) NULL,
    [EventTime] datetime2 NOT NULL,
    [SyncTime] datetime2 NULL,
    [IsSynced] bit NOT NULL,
    [SyncVersion] int NOT NULL,
    CONSTRAINT [PK_Tasks] PRIMARY KEY ([TaskId]),
    CONSTRAINT [FK_Tasks_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL,
    CONSTRAINT [FK_Tasks_Users_AssignedToUserId] FOREIGN KEY ([AssignedToUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_Tasks_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [Zones] ([ZoneId]) ON DELETE SET NULL
);

CREATE TABLE [UserZones] (
    [UserId] int NOT NULL,
    [ZoneId] int NOT NULL,
    [AssignedAt] datetime2 NOT NULL,
    [AssignedByUserId] int NOT NULL,
    [IsActive] bit NOT NULL,
    CONSTRAINT [PK_UserZones] PRIMARY KEY ([UserId], [ZoneId]),
    CONSTRAINT [FK_UserZones_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE,
    CONSTRAINT [FK_UserZones_Zones_ZoneId] FOREIGN KEY ([ZoneId]) REFERENCES [Zones] ([ZoneId]) ON DELETE CASCADE
);

CREATE INDEX [IX_Attendance_User_CheckIn] ON [Attendances] ([UserId], [CheckInEventTime]);

CREATE INDEX [IX_Attendances_Status] ON [Attendances] ([Status]);

CREATE INDEX [IX_Attendances_ZoneId] ON [Attendances] ([ZoneId]);

CREATE INDEX [IX_AuditLog_User_Timestamp] ON [AuditLogs] ([UserId], [Timestamp]);

CREATE INDEX [IX_AuditLogs_EntityType] ON [AuditLogs] ([EntityType]);

CREATE INDEX [IX_AuditLogs_Timestamp] ON [AuditLogs] ([Timestamp]);

CREATE INDEX [IX_Issue_Reporter_Status] ON [Issues] ([ReportedByUserId], [Status]);

CREATE INDEX [IX_Issues_ReportedAt] ON [Issues] ([ReportedAt]);

CREATE INDEX [IX_Issues_ResolvedByUserId] ON [Issues] ([ResolvedByUserId]);

CREATE INDEX [IX_Issues_Severity] ON [Issues] ([Severity]);

CREATE INDEX [IX_Issues_Type] ON [Issues] ([Type]);

CREATE INDEX [IX_Issues_ZoneId] ON [Issues] ([ZoneId]);

CREATE INDEX [IX_Notification_User_IsRead] ON [Notifications] ([UserId], [IsRead]);

CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications] ([CreatedAt]);

CREATE INDEX [IX_Notifications_IsSent] ON [Notifications] ([IsSent]);

CREATE INDEX [IX_Notifications_Type] ON [Notifications] ([Type]);

CREATE INDEX [IX_SyncLog_Entity] ON [SyncLogs] ([EntityType], [EntityId]);

CREATE INDEX [IX_SyncLog_User_SyncTime] ON [SyncLogs] ([UserId], [SyncTime]);

CREATE INDEX [IX_SyncLogs_HadConflict] ON [SyncLogs] ([HadConflict]);

CREATE INDEX [IX_Task_AssignedUser_Status] ON [Tasks] ([AssignedToUserId], [Status]);

CREATE INDEX [IX_Tasks_AssignedByUserId] ON [Tasks] ([AssignedByUserId]);

CREATE INDEX [IX_Tasks_DueDate] ON [Tasks] ([DueDate]);

CREATE INDEX [IX_Tasks_Priority] ON [Tasks] ([Priority]);

CREATE INDEX [IX_Tasks_ZoneId] ON [Tasks] ([ZoneId]);

CREATE INDEX [IX_Users_Email] ON [Users] ([Email]);

CREATE UNIQUE INDEX [IX_Users_Username] ON [Users] ([Username]);

CREATE INDEX [IX_UserZone_IsActive] ON [UserZones] ([IsActive]);

CREATE INDEX [IX_UserZone_UserId] ON [UserZones] ([UserId]);

CREATE INDEX [IX_UserZone_ZoneId] ON [UserZones] ([ZoneId]);

CREATE INDEX [IX_Zone_IsActive] ON [Zones] ([IsActive]);

CREATE UNIQUE INDEX [IX_Zone_ZoneCode_Unique] ON [Zones] ([ZoneCode]);

CREATE INDEX [IX_Zones_District] ON [Zones] ([District]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251109183404_InitialCreate', N'9.0.10');

ALTER TABLE [Users] ADD [Pin] nvarchar(4) NULL;

CREATE UNIQUE INDEX [IX_Users_Pin] ON [Users] ([Pin]) WHERE [Pin] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251121151006_AddUserPin', N'9.0.10');

CREATE TABLE [RefreshTokens] (
    [Id] int NOT NULL IDENTITY,
    [Token] nvarchar(500) NOT NULL,
    [UserId] int NOT NULL,
    [ExpiresAt] datetime2 NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [RevokedAt] datetime2 NULL,
    [ReplacedByToken] nvarchar(500) NULL,
    [DeviceInfo] nvarchar(500) NULL,
    [IpAddress] nvarchar(50) NULL,
    CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_RefreshTokens_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE CASCADE
);

CREATE UNIQUE INDEX [IX_RefreshTokens_Token] ON [RefreshTokens] ([Token]);

CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251121151705_AddRefreshToken', N'9.0.10');

DECLARE @var sysname;
SELECT @var = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Attendances]') AND [c].[name] = N'DeviceId');
IF @var IS NOT NULL EXEC(N'ALTER TABLE [Attendances] DROP CONSTRAINT [' + @var + '];');
ALTER TABLE [Attendances] DROP COLUMN [DeviceId];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251122222941_RemoveAttendanceDeviceId', N'9.0.10');

CREATE TABLE [LocationHistories] (
    [Id] int NOT NULL IDENTITY,
    [UserId] int NOT NULL,
    [Latitude] float NOT NULL,
    [Longitude] float NOT NULL,
    [Speed] float NULL,
    [Accuracy] float NULL,
    [Heading] float NULL,
    [Timestamp] datetime2 NOT NULL,
    [IsSync] bit NOT NULL,
    CONSTRAINT [PK_LocationHistories] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_LocationHistories_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_LocationHistories_UserId_Timestamp] ON [LocationHistories] ([UserId], [Timestamp]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251206194828_AddLocationHistoryForPhase4', N'9.0.10');

ALTER TABLE [Users] ADD [FcmToken] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251206204537_AddFcmTokenToUser', N'9.0.10');

CREATE INDEX [IX_Users_Role] ON [Users] ([Role]);

CREATE INDEX [IX_Users_Role_Status] ON [Users] ([Role], [Status]);

CREATE INDEX [IX_Users_Status] ON [Users] ([Status]);

CREATE INDEX [IX_Tasks_AssignedToUserId] ON [Tasks] ([AssignedToUserId]);

CREATE INDEX [IX_Tasks_Status] ON [Tasks] ([Status]);

CREATE INDEX [IX_Tasks_SyncTime] ON [Tasks] ([SyncTime]);

CREATE INDEX [IX_Issues_ReportedByUserId] ON [Issues] ([ReportedByUserId]);

CREATE INDEX [IX_Issues_Status] ON [Issues] ([Status]);

CREATE INDEX [IX_Attendances_CheckInEventTime] ON [Attendances] ([CheckInEventTime]);

CREATE INDEX [IX_Attendances_UserId] ON [Attendances] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251220170109_AddPerformanceIndices', N'9.0.10');

ALTER TABLE [Tasks] ADD [TaskType] int NULL;

ALTER TABLE [Tasks] ADD [RequiresPhotoProof] bit NOT NULL DEFAULT CAST(1 AS bit);

ALTER TABLE [Tasks] ADD [EstimatedDurationMinutes] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251221115700_AddTaskEnhancements', N'9.0.10');

CREATE TABLE [Photos] (
    [PhotoId] int NOT NULL IDENTITY,
    [PhotoUrl] nvarchar(500) NOT NULL,
    [EntityType] nvarchar(50) NOT NULL,
    [EntityId] int NOT NULL,
    [OrderIndex] int NOT NULL DEFAULT 0,
    [FileSizeBytes] bigint NULL,
    [UploadedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [UploadedByUserId] int NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT (GETUTCDATE()),
    [IssueId] int NULL,
    [TaskId] int NULL,
    CONSTRAINT [PK_Photos] PRIMARY KEY ([PhotoId]),
    CONSTRAINT [FK_Photos_Issues_IssueId] FOREIGN KEY ([IssueId]) REFERENCES [Issues] ([IssueId]),
    CONSTRAINT [FK_Photos_Tasks_TaskId] FOREIGN KEY ([TaskId]) REFERENCES [Tasks] ([TaskId]),
    CONSTRAINT [FK_Photos_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE SET NULL
);

CREATE INDEX [IX_Photos_EntityType_EntityId] ON [Photos] ([EntityType], [EntityId]);

CREATE INDEX [IX_Photos_IssueId] ON [Photos] ([IssueId]);

CREATE INDEX [IX_Photos_TaskId] ON [Photos] ([TaskId]);

CREATE INDEX [IX_Photos_UploadedAt] ON [Photos] ([UploadedAt]);

CREATE INDEX [IX_Photos_UploadedByUserId] ON [Photos] ([UploadedByUserId]);


                -- Migrate Issue Photos from PhotoUrl field
                INSERT INTO Photos (PhotoUrl, EntityType, EntityId, OrderIndex, UploadedAt, IssueId)
                SELECT
                    LTRIM(RTRIM(value)) AS PhotoUrl,
                    'Issue' AS EntityType,
                    i.IssueId AS EntityId,
                    ROW_NUMBER() OVER (PARTITION BY i.IssueId ORDER BY (SELECT NULL)) - 1 AS OrderIndex,
                    i.ReportedAt AS UploadedAt,
                    i.IssueId
                FROM Issues i
                CROSS APPLY STRING_SPLIT(i.PhotoUrl, ';')
                WHERE i.PhotoUrl IS NOT NULL AND i.PhotoUrl != '' AND LTRIM(RTRIM(value)) != '';

                -- Migrate Issue Photos from AdditionalPhotosJson field (if any)
                INSERT INTO Photos (PhotoUrl, EntityType, EntityId, OrderIndex, UploadedAt, IssueId)
                SELECT
                    LTRIM(RTRIM(value)) AS PhotoUrl,
                    'Issue' AS EntityType,
                    i.IssueId AS EntityId,
                    (SELECT COUNT(*) FROM Photos WHERE EntityType = 'Issue' AND EntityId = i.IssueId) + ROW_NUMBER() OVER (PARTITION BY i.IssueId ORDER BY (SELECT NULL)) - 1 AS OrderIndex,
                    i.ReportedAt AS UploadedAt,
                    i.IssueId
                FROM Issues i
                CROSS APPLY STRING_SPLIT(i.AdditionalPhotosJson, ';')
                WHERE i.AdditionalPhotosJson IS NOT NULL AND i.AdditionalPhotosJson != '' AND LTRIM(RTRIM(value)) != '';

                -- Migrate Task Photos from PhotoUrl field
                INSERT INTO Photos (PhotoUrl, EntityType, EntityId, OrderIndex, UploadedAt, TaskId)
                SELECT
                    LTRIM(RTRIM(value)) AS PhotoUrl,
                    'Task' AS EntityType,
                    t.TaskId AS EntityId,
                    ROW_NUMBER() OVER (PARTITION BY t.TaskId ORDER BY (SELECT NULL)) - 1 AS OrderIndex,
                    COALESCE(t.CompletedAt, t.CreatedAt) AS UploadedAt,
                    t.TaskId
                FROM Tasks t
                CROSS APPLY STRING_SPLIT(t.PhotoUrl, ';')
                WHERE t.PhotoUrl IS NOT NULL AND t.PhotoUrl != '' AND LTRIM(RTRIM(value)) != '';
            

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251226193332_AddPhotosTables', N'9.0.10');

CREATE UNIQUE INDEX [IX_Users_Pin_Unique] ON [Users] ([Pin]) WHERE [Pin] IS NOT NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251227123906_AddUniquePinConstraint', N'9.0.10');

ALTER TABLE [Tasks] ADD [RowVersion] rowversion NULL;

ALTER TABLE [Issues] ADD [RowVersion] rowversion NULL;

CREATE TABLE [FileMetadata] (
    [Id] int NOT NULL IDENTITY,
    [OriginalFileName] nvarchar(255) NOT NULL,
    [StoredFileName] nvarchar(255) NOT NULL,
    [FilePath] nvarchar(500) NOT NULL,
    [FileUrl] nvarchar(500) NOT NULL,
    [FileSizeBytes] bigint NOT NULL,
    [ContentType] nvarchar(100) NOT NULL,
    [EntityType] nvarchar(50) NOT NULL,
    [EntityId] int NULL,
    [UploadedByUserId] int NOT NULL,
    [UploadedAt] datetime2 NOT NULL,
    [MarkedForDeletionAt] datetime2 NULL,
    [IsOrphaned] bit NOT NULL,
    [IsDeleted] bit NOT NULL,
    CONSTRAINT [PK_FileMetadata] PRIMARY KEY ([Id]),
    CONSTRAINT [FK_FileMetadata_Users_UploadedByUserId] FOREIGN KEY ([UploadedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);

CREATE INDEX [IX_FileMetadata_EntityId] ON [FileMetadata] ([EntityId]);

CREATE INDEX [IX_FileMetadata_EntityType] ON [FileMetadata] ([EntityType]);

CREATE INDEX [IX_FileMetadata_EntityType_EntityId_IsDeleted] ON [FileMetadata] ([EntityType], [EntityId], [IsDeleted]);

CREATE INDEX [IX_FileMetadata_IsDeleted] ON [FileMetadata] ([IsDeleted]);

CREATE INDEX [IX_FileMetadata_IsOrphaned] ON [FileMetadata] ([IsOrphaned]);

CREATE INDEX [IX_FileMetadata_MarkedForDeletionAt] ON [FileMetadata] ([MarkedForDeletionAt]);

CREATE INDEX [IX_FileMetadata_UploadedAt] ON [FileMetadata] ([UploadedAt]);

CREATE INDEX [IX_FileMetadata_UploadedByUserId] ON [FileMetadata] ([UploadedByUserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251227172412_AddRowVersionConcurrency', N'9.0.10');

DROP TABLE [AuditLogs];

DROP TABLE [FileMetadata];

DROP TABLE [SyncLogs];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251229002237_RemoveUnusedTables', N'9.0.10');

DECLARE @var1 sysname;
SELECT @var1 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'DeviceInfo');
IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT [' + @var1 + '];');
ALTER TABLE [RefreshTokens] DROP COLUMN [DeviceInfo];

DECLARE @var2 sysname;
SELECT @var2 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'IpAddress');
IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT [' + @var2 + '];');
ALTER TABLE [RefreshTokens] DROP COLUMN [IpAddress];

DECLARE @var3 sysname;
SELECT @var3 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[RefreshTokens]') AND [c].[name] = N'ReplacedByToken');
IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [RefreshTokens] DROP CONSTRAINT [' + @var3 + '];');
ALTER TABLE [RefreshTokens] DROP COLUMN [ReplacedByToken];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20251229002548_SimplifyRefreshToken', N'9.0.10');

DROP INDEX [IX_Users_Role] ON [Users];

DROP INDEX [IX_Users_Role_Status] ON [Users];

DROP INDEX [IX_Users_Status] ON [Users];

DROP INDEX [IX_Tasks_AssignedToUserId] ON [Tasks];

DROP INDEX [IX_Tasks_Status] ON [Tasks];

DROP INDEX [IX_Tasks_SyncTime] ON [Tasks];

DROP INDEX [IX_Issues_ReportedByUserId] ON [Issues];

DROP INDEX [IX_Issues_Status] ON [Issues];

DROP INDEX [IX_Attendances_CheckInEventTime] ON [Attendances];

DROP INDEX [IX_Attendances_UserId] ON [Attendances];

DECLARE @var4 sysname;
SELECT @var4 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Notifications]') AND [c].[name] = N'FcmToken');
IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Notifications] DROP CONSTRAINT [' + @var4 + '];');
ALTER TABLE [Notifications] DROP COLUMN [FcmToken];

DECLARE @var5 sysname;
SELECT @var5 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Issues]') AND [c].[name] = N'AdditionalPhotosJson');
IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Issues] DROP CONSTRAINT [' + @var5 + '];');
ALTER TABLE [Issues] DROP COLUMN [AdditionalPhotosJson];

EXEC sp_rename N'[LocationHistories].[IX_LocationHistories_UserId_Timestamp]', N'IX_LocationHistory_User_Timestamp', 'INDEX';

EXEC sp_rename N'[Attendances].[IX_Attendances_ZoneId]', N'IX_Attendance_ZoneId', 'INDEX';

EXEC sp_rename N'[Attendances].[IX_Attendances_Status]', N'IX_Attendance_Status', 'INDEX';

DECLARE @var6 sysname;
SELECT @var6 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'FcmToken');
IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var6 + '];');
ALTER TABLE [Users] ALTER COLUMN [FcmToken] nvarchar(255) NULL;

DECLARE @var7 sysname;
SELECT @var7 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationHistories]') AND [c].[name] = N'Longitude');
IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [LocationHistories] DROP CONSTRAINT [' + @var7 + '];');
ALTER TABLE [LocationHistories] ALTER COLUMN [Longitude] float(18) NOT NULL;

DECLARE @var8 sysname;
SELECT @var8 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[LocationHistories]') AND [c].[name] = N'Latitude');
IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [LocationHistories] DROP CONSTRAINT [' + @var8 + '];');
ALTER TABLE [LocationHistories] ALTER COLUMN [Latitude] float(18) NOT NULL;

CREATE INDEX [IX_UserZones_AssignedByUserId] ON [UserZones] ([AssignedByUserId]);

ALTER TABLE [UserZones] ADD CONSTRAINT [FK_UserZones_Users_AssignedByUserId] FOREIGN KEY ([AssignedByUserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260112012631_SyncSchemaCleanup', N'9.0.10');

ALTER TABLE [Users] ADD [RegisteredDeviceId] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260114123541_AddUserRegisteredDeviceId', N'9.0.10');

DROP TABLE [IssueForwardings];

DROP TABLE [ExternalDepartments];

ALTER TABLE [Issues] ADD [ForwardingNotes] nvarchar(max) NULL;

ALTER TABLE [Issues] ADD [PdfDownloadedAt] datetime2 NULL;

ALTER TABLE [Issues] ADD [PdfDownloadedByUserId] int NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260114214110_ReplaceForwardingWithPdfDownload', N'9.0.10');

ALTER TABLE [Users] ADD [FailedLoginAttempts] int NOT NULL DEFAULT 0;

ALTER TABLE [Users] ADD [LockoutEndTime] datetime2 NULL;

ALTER TABLE [Attendances] ADD [ApprovedAt] datetime2 NULL;

ALTER TABLE [Attendances] ADD [ApprovedByUserId] int NULL;

ALTER TABLE [Attendances] ADD [IsManual] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Attendances] ADD [ManualReason] nvarchar(max) NULL;

CREATE TABLE [AuditLogs] (
    [AuditLogId] int NOT NULL IDENTITY,
    [UserId] int NULL,
    [Username] nvarchar(max) NULL,
    [Action] nvarchar(max) NOT NULL,
    [Details] nvarchar(max) NULL,
    [IpAddress] nvarchar(max) NULL,
    [UserAgent] nvarchar(max) NULL,
    [CreatedAt] datetime2 NOT NULL,
    CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([AuditLogId]),
    CONSTRAINT [FK_AuditLogs_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId])
);

CREATE INDEX [IX_Attendances_ApprovedByUserId] ON [Attendances] ([ApprovedByUserId]);

CREATE INDEX [IX_AuditLogs_UserId] ON [AuditLogs] ([UserId]);

ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Users_ApprovedByUserId] FOREIGN KEY ([ApprovedByUserId]) REFERENCES [Users] ([UserId]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260114222121_SecurityAndAuditFeatures', N'9.0.10');

ALTER TABLE [Users] ADD [ConsentVersion] int NOT NULL DEFAULT 0;

ALTER TABLE [Users] ADD [ExpectedEndTime] time NOT NULL DEFAULT '16:00:00';

ALTER TABLE [Users] ADD [ExpectedStartTime] time NOT NULL DEFAULT '08:00:00';

ALTER TABLE [Users] ADD [GraceMinutes] int NOT NULL DEFAULT 15;

ALTER TABLE [Users] ADD [PrivacyConsentedAt] datetime2 NULL;

ALTER TABLE [Tasks] ADD [CompletionDistanceMeters] int NULL;

ALTER TABLE [Tasks] ADD [IsDistanceWarning] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Tasks] ADD [MaxDistanceMeters] int NOT NULL DEFAULT 100;

ALTER TABLE [Attendances] ADD [ApprovalStatus] nvarchar(50) NOT NULL DEFAULT N'AutoApproved';

ALTER TABLE [Attendances] ADD [AttendanceType] nvarchar(50) NOT NULL DEFAULT N'OnTime';

ALTER TABLE [Attendances] ADD [EarlyLeaveMinutes] int NOT NULL DEFAULT 0;

ALTER TABLE [Attendances] ADD [LateMinutes] int NOT NULL DEFAULT 0;

ALTER TABLE [Attendances] ADD [OvertimeMinutes] int NOT NULL DEFAULT 0;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260117001901_EvaluationReadyFeatures', N'9.0.10');

CREATE TABLE [Municipalities] (
    [MunicipalityId] int NOT NULL IDENTITY,
    [Code] nvarchar(50) NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [NameEnglish] nvarchar(200) NULL,
    [Country] nvarchar(100) NOT NULL,
    [Region] nvarchar(100) NULL,
    [ContactEmail] nvarchar(100) NULL,
    [ContactPhone] nvarchar(50) NULL,
    [Address] nvarchar(500) NULL,
    [LogoUrl] nvarchar(500) NULL,
    [MinLatitude] float(18) NOT NULL,
    [MaxLatitude] float(18) NOT NULL,
    [MinLongitude] float(18) NOT NULL,
    [MaxLongitude] float(18) NOT NULL,
    [DefaultStartTime] time NOT NULL,
    [DefaultEndTime] time NOT NULL,
    [DefaultGraceMinutes] int NOT NULL DEFAULT 15,
    [MaxAcceptableAccuracyMeters] float NOT NULL DEFAULT 150.0E0,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [LicenseExpiresAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL,
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK_Municipalities] PRIMARY KEY ([MunicipalityId])
);

CREATE UNIQUE INDEX [IX_Municipality_Code_Unique] ON [Municipalities] ([Code]);

CREATE INDEX [IX_Municipality_IsActive] ON [Municipalities] ([IsActive]);


                INSERT INTO Municipalities (Code, Name, NameEnglish, Country, Region,
                    MinLatitude, MaxLatitude, MinLongitude, MaxLongitude,
                    DefaultStartTime, DefaultEndTime, DefaultGraceMinutes, MaxAcceptableAccuracyMeters,
                    IsActive, CreatedAt)
                VALUES ('ALBIREH', N'بلدية البيرة', 'Al-Bireh Municipality', 'Palestine', N'رام الله والبيرة',
                    31.87, 31.95, 35.18, 35.27,
                    '08:00:00', '16:00:00', 15, 150.0,
                    1, GETUTCDATE())
            

ALTER TABLE [Zones] ADD [MunicipalityId] int NOT NULL DEFAULT 1;

ALTER TABLE [Users] ADD [MunicipalityId] int NOT NULL DEFAULT 1;

ALTER TABLE [Tasks] ADD [MunicipalityId] int NOT NULL DEFAULT 1;

ALTER TABLE [Notifications] ADD [MunicipalityId] int NOT NULL DEFAULT 1;

ALTER TABLE [Issues] ADD [MunicipalityId] int NOT NULL DEFAULT 1;

ALTER TABLE [Attendances] ADD [MunicipalityId] int NOT NULL DEFAULT 1;

CREATE INDEX [IX_Zones_MunicipalityId] ON [Zones] ([MunicipalityId]);

CREATE INDEX [IX_Users_MunicipalityId] ON [Users] ([MunicipalityId]);

CREATE INDEX [IX_Tasks_MunicipalityId] ON [Tasks] ([MunicipalityId]);

CREATE INDEX [IX_Notifications_MunicipalityId] ON [Notifications] ([MunicipalityId]);

CREATE INDEX [IX_Issues_MunicipalityId] ON [Issues] ([MunicipalityId]);

CREATE INDEX [IX_Attendances_MunicipalityId] ON [Attendances] ([MunicipalityId]);

ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Municipalities_MunicipalityId] FOREIGN KEY ([MunicipalityId]) REFERENCES [Municipalities] ([MunicipalityId]) ON DELETE NO ACTION;

ALTER TABLE [Issues] ADD CONSTRAINT [FK_Issues_Municipalities_MunicipalityId] FOREIGN KEY ([MunicipalityId]) REFERENCES [Municipalities] ([MunicipalityId]) ON DELETE NO ACTION;

ALTER TABLE [Notifications] ADD CONSTRAINT [FK_Notifications_Municipalities_MunicipalityId] FOREIGN KEY ([MunicipalityId]) REFERENCES [Municipalities] ([MunicipalityId]) ON DELETE NO ACTION;

ALTER TABLE [Tasks] ADD CONSTRAINT [FK_Tasks_Municipalities_MunicipalityId] FOREIGN KEY ([MunicipalityId]) REFERENCES [Municipalities] ([MunicipalityId]) ON DELETE NO ACTION;

ALTER TABLE [Users] ADD CONSTRAINT [FK_Users_Municipalities_MunicipalityId] FOREIGN KEY ([MunicipalityId]) REFERENCES [Municipalities] ([MunicipalityId]) ON DELETE NO ACTION;

ALTER TABLE [Zones] ADD CONSTRAINT [FK_Zones_Municipalities_MunicipalityId] FOREIGN KEY ([MunicipalityId]) REFERENCES [Municipalities] ([MunicipalityId]) ON DELETE NO ACTION;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260117004028_MultiMunicipalitySupport', N'9.0.10');

ALTER TABLE [Users] ADD [LastWarningAt] datetime2 NULL;

ALTER TABLE [Users] ADD [LastWarningReason] nvarchar(max) NULL;

ALTER TABLE [Users] ADD [WarningCount] int NOT NULL DEFAULT 0;

ALTER TABLE [Tasks] ADD [ExtendedByUserId] int NULL;

ALTER TABLE [Tasks] ADD [ExtendedDeadline] datetime2 NULL;

ALTER TABLE [Tasks] ADD [IsAutoRejected] bit NOT NULL DEFAULT CAST(0 AS bit);

ALTER TABLE [Tasks] ADD [ProgressNotes] nvarchar(max) NULL;

ALTER TABLE [Tasks] ADD [ProgressPercentage] int NOT NULL DEFAULT 0;

ALTER TABLE [Tasks] ADD [RejectedAt] datetime2 NULL;

ALTER TABLE [Tasks] ADD [RejectedByUserId] int NULL;

ALTER TABLE [Tasks] ADD [RejectionDistanceMeters] int NULL;

ALTER TABLE [Tasks] ADD [RejectionLatitude] float NULL;

ALTER TABLE [Tasks] ADD [RejectionLongitude] float NULL;

ALTER TABLE [Tasks] ADD [RejectionReason] nvarchar(max) NULL;

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260117010937_TaskProgressAndRejectionTracking', N'9.0.10');

DROP TABLE [RefreshTokens];

DECLARE @var9 sysname;
SELECT @var9 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'ConsentVersion');
IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var9 + '];');
ALTER TABLE [Users] DROP COLUMN [ConsentVersion];

DECLARE @var10 sysname;
SELECT @var10 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Users]') AND [c].[name] = N'PrivacyConsentedAt');
IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [Users] DROP CONSTRAINT [' + @var10 + '];');
ALTER TABLE [Users] DROP COLUMN [PrivacyConsentedAt];

DECLARE @var11 sysname;
SELECT @var11 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Issues]') AND [c].[name] = N'ForwardingNotes');
IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [Issues] DROP CONSTRAINT [' + @var11 + '];');
ALTER TABLE [Issues] DROP COLUMN [ForwardingNotes];

DECLARE @var12 sysname;
SELECT @var12 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Issues]') AND [c].[name] = N'PdfDownloadedAt');
IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [Issues] DROP CONSTRAINT [' + @var12 + '];');
ALTER TABLE [Issues] DROP COLUMN [PdfDownloadedAt];

DECLARE @var13 sysname;
SELECT @var13 = [d].[name]
FROM [sys].[default_constraints] [d]
INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Issues]') AND [c].[name] = N'PdfDownloadedByUserId');
IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [Issues] DROP CONSTRAINT [' + @var13 + '];');
ALTER TABLE [Issues] DROP COLUMN [PdfDownloadedByUserId];

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260117160006_RemoveUnusedFeatures', N'9.0.10');

COMMIT;
GO

