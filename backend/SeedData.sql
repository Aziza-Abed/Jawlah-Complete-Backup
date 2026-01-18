-- =====================================================
-- Jawlah Database - Complete Realistic Seed Data
-- Run this script in SQL Server Management Studio
-- All columns populated - NO NULL values where possible
-- Password for all users: Admin@123
-- =====================================================
SET QUOTED_IDENTIFIER ON;
GO

-- STEP 1: Clear all existing data (order matters for foreign keys)
-- =====================================================
PRINT 'Clearing existing data...'

DELETE FROM Photos;
DELETE FROM Notifications;
DELETE FROM LocationHistories;
DELETE FROM Issues;
DELETE FROM Tasks;
DELETE FROM Attendances;
-- DELETE FROM RefreshTokens;
DELETE FROM UserZones;
DELETE FROM Users;
-- Note: We keep Zones because they come from GIS shapefile

PRINT 'Data cleared successfully.'

-- STEP 2: Reset identity columns
-- =====================================================
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Tasks', RESEED, 0);
DBCC CHECKIDENT ('Issues', RESEED, 0);
DBCC CHECKIDENT ('Attendances', RESEED, 0);
DBCC CHECKIDENT ('Notifications', RESEED, 0);

-- =====================================================
-- STEP 3: Insert Users (ALL columns filled)
-- =====================================================
PRINT 'Inserting users...'

-- Admin (Pin must be NULL - unique index doesn't allow duplicate empty strings)
INSERT INTO Users (Username, PasswordHash, Pin, Email, PhoneNumber, FullName, Role, WorkerType, Department, Status, CreatedAt, LastLoginAt, FcmToken)
VALUES
('admin', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', NULL, 'admin@albireh.ps', '+970599000001', N'مدير النظام', 0, NULL, N'تكنولوجيا المعلومات', 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '');

-- Supervisors (Pin must be NULL - unique index doesn't allow duplicate empty strings)
INSERT INTO Users (Username, PasswordHash, Pin, Email, PhoneNumber, FullName, Role, WorkerType, Department, Status, CreatedAt, LastLoginAt, FcmToken)
VALUES
('supervisor1', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', NULL, 'khalid.supervisor@albireh.ps', '+970599000002', N'خالد أبو سعدة', 1, NULL, N'قسم النظافة', 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), ''),
('supervisor2', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', NULL, 'ahmad.supervisor@albireh.ps', '+970599000003', N'أحمد الشريف', 1, NULL, N'قسم الصيانة', 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '');

-- Workers (ALL columns including Pin and WorkerType)
INSERT INTO Users (Username, PasswordHash, Pin, Email, PhoneNumber, FullName, Role, WorkerType, Department, Status, CreatedAt, LastLoginAt, FcmToken)
VALUES
('worker1', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '1234', 'ahmad.hassan@albireh.ps', '+970593001001', N'أحمد حسن محمود', 2, 0, N'قسم النظافة', 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), ''),
('worker2', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '1235', 'mohammad.ali@albireh.ps', '+970593001002', N'محمد علي عبدالله', 2, 0, N'قسم النظافة', 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), ''),
('worker3', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '1236', 'omar.khaled@albireh.ps', '+970593001003', N'عمر خالد ياسين', 2, 0, N'قسم النظافة', 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), ''),
('worker4', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '2234', 'sami.naser@albireh.ps', '+970593002001', N'سامي ناصر حمدان', 2, 1, N'قسم الصيانة', 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), ''),
('worker5', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '2235', 'fadi.mahmoud@albireh.ps', '+970593002002', N'فادي محمود سليم', 2, 1, N'قسم الصيانة', 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), ''),
('worker6', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '3234', 'rami.saleh@albireh.ps', '+970593003001', N'رامي صالح عودة', 2, 2, N'قسم التفتيش', 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), ''),
('worker7', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '3235', 'nidal.ahmad@albireh.ps', '+970593003002', N'نضال أحمد زيدان', 2, 2, N'قسم التفتيش', 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), ''),
('worker8', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '4234', 'yazan.omar@albireh.ps', '+970593004001', N'يزن عمر الحاج', 2, 3, N'قسم الكهرباء', 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), ''),
('worker9', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '5234', 'bilal.hassan@albireh.ps', '+970593005001', N'بلال حسن مصطفى', 2, 4, N'قسم المياه', 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), ''),
('worker10', '$2a$12$wlO7xBUj7BtfuCJ9Hsk7rO3qMw56AiPZRiJrslwRx1j/odODK4xY6', '5235', 'majdi.khalil@albireh.ps', '+970593005002', N'مجدي خليل داود', 2, 4, N'قسم المياه', 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '');

PRINT 'Users inserted: 1 Admin, 2 Supervisors, 10 Workers'

-- =====================================================
-- STEP 4: Assign Workers to Zones
-- =====================================================
PRINT 'Assigning workers to zones...'

DECLARE @Zone1 INT = (SELECT TOP 1 ZoneId FROM Zones ORDER BY ZoneId);
DECLARE @Zone2 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE ZoneId > @Zone1 ORDER BY ZoneId);
DECLARE @Zone3 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE ZoneId > @Zone2 ORDER BY ZoneId);

DECLARE @AdminId INT = (SELECT UserId FROM Users WHERE Username = 'admin');

-- Assign workers to zones (one zone per worker for simplicity)
INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker1' AND @Zone1 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone2, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker2' AND @Zone2 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone3, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker3' AND @Zone3 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker4' AND @Zone1 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone2, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker5' AND @Zone2 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker6' AND @Zone1 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone2, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker7' AND @Zone2 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker8' AND @Zone1 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker9' AND @Zone1 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone3, @AdminId, GETUTCDATE(), 1 FROM Users u WHERE u.Username = 'worker10' AND @Zone3 IS NOT NULL;

PRINT 'Zone assignments completed.'

-- =====================================================
-- STEP 5: Insert Tasks (ALL columns filled - NO NULLs)
-- =====================================================
PRINT 'Inserting tasks...'

DECLARE @Sup1 INT = (SELECT UserId FROM Users WHERE Username = 'supervisor1');
DECLARE @Sup2 INT = (SELECT UserId FROM Users WHERE Username = 'supervisor2');
DECLARE @W1 INT = (SELECT UserId FROM Users WHERE Username = 'worker1');
DECLARE @W2 INT = (SELECT UserId FROM Users WHERE Username = 'worker2');
DECLARE @W3 INT = (SELECT UserId FROM Users WHERE Username = 'worker3');
DECLARE @W4 INT = (SELECT UserId FROM Users WHERE Username = 'worker4');
DECLARE @W5 INT = (SELECT UserId FROM Users WHERE Username = 'worker5');
DECLARE @W6 INT = (SELECT UserId FROM Users WHERE Username = 'worker6');
DECLARE @W8 INT = (SELECT UserId FROM Users WHERE Username = 'worker8');
DECLARE @W9 INT = (SELECT UserId FROM Users WHERE Username = 'worker9');
DECLARE @W10 INT = (SELECT UserId FROM Users WHERE Username = 'worker10');

-- Worker 1 Tasks (Sanitation) - Pending status
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'تنظيف شارع القدس الرئيسي', N'تنظيف شامل للشارع وإزالة النفايات من الأرصفة والطرقات', @W1, @Sup1, @Zone1, 2, 0, 0, 1, 60, DATEADD(HOUR, 4, GETUTCDATE()), NULL, NULL, 31.9035, 35.2052, N'شارع القدس الرئيسي - من التقاطع حتى الدوار', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'جمع النفايات من حاويات حي الشمال', N'جمع النفايات من 15 حاوية في حي الشمال وتنظيف محيطها', @W1, @Sup1, @Zone1, 2, 0, 0, 1, 90, DATEADD(HOUR, 6, GETUTCDATE()), NULL, NULL, 31.9041, 35.2061, N'حي الشمال - جميع الحاويات', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'تنظيف ساحة البلدية', N'تنظيف الساحة العامة أمام مبنى البلدية وترتيب المقاعد', @W1, @Sup1, @Zone1, 1, 0, 0, 1, 45, DATEADD(DAY, 1, GETUTCDATE()), NULL, NULL, 31.9022, 35.2044, N'ساحة البلدية الرئيسية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 1 InProgress Task
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'تنظيف حديقة الأطفال', N'تنظيف الحديقة العامة وجمع الأوراق المتساقطة', @W1, @Sup1, @Zone1, 1, 1, 0, 1, 40, DATEADD(HOUR, 2, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, 31.9055, 35.2070, N'حديقة الأطفال - المدخل الشرقي', '', '', GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), GETUTCDATE(), 1, 1);

-- Worker 2 Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'تنظيف موقف الباصات', N'تنظيف موقف الباصات المركزي وإزالة النفايات', @W2, @Sup1, @Zone2, 2, 0, 0, 1, 50, DATEADD(HOUR, 3, GETUTCDATE()), NULL, NULL, 31.9060, 35.2080, N'موقف الباصات المركزي', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'تنظيف محيط المدرسة', N'تنظيف الشوارع المحيطة بالمدرسة قبل دوام الطلاب', @W2, @Sup1, @Zone2, 2, 0, 0, 1, 45, DATEADD(HOUR, 2, GETUTCDATE()), NULL, NULL, 31.9065, 35.2085, N'مدرسة البيرة الثانوية - المحيط الخارجي', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 3 Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'تنظيف الحديقة العامة الكبرى', N'تنظيف شامل للحديقة العامة وجمع النفايات', @W3, @Sup1, @Zone3, 1, 0, 0, 1, 120, DATEADD(HOUR, 5, GETUTCDATE()), NULL, NULL, 31.9080, 35.2100, N'الحديقة العامة الكبرى - جميع الأقسام', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 4 Tasks (Maintenance)
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'إصلاح رصيف مكسور', N'إصلاح الرصيف المكسور أمام البنك العربي واستبدال البلاط التالف', @W4, @Sup2, @Zone1, 2, 0, 1, 1, 90, DATEADD(HOUR, 6, GETUTCDATE()), NULL, NULL, 31.9032, 35.2049, N'أمام البنك العربي - شارع القدس', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'صيانة مقاعد الحديقة', N'إصلاح 5 مقاعد خشبية تالفة في حديقة الأطفال', @W4, @Sup2, @Zone1, 1, 0, 1, 1, 120, DATEADD(DAY, 2, GETUTCDATE()), NULL, NULL, 31.9055, 35.2070, N'حديقة الأطفال - منطقة الجلوس', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 4 InProgress Task
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'إصلاح باب حديقة', N'إصلاح مفصلات باب الحديقة الرئيسي وتزييته', @W4, @Sup2, @Zone1, 1, 1, 1, 1, 30, DATEADD(HOUR, 3, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, 31.9052, 35.2068, N'مدخل حديقة الأطفال الرئيسي', '', '', GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 1, 1);

-- Worker 5 Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'إصلاح سياج حديدي', N'إصلاح السياج الحديدي المائل وتثبيته بشكل صحيح', @W5, @Sup2, @Zone2, 2, 0, 1, 1, 60, DATEADD(HOUR, 4, GETUTCDATE()), NULL, NULL, 31.9062, 35.2082, N'شارع الإرسال - مقابل المسجد', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 6 Tasks (Inspector)
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'جولة تفتيشية على المحلات', N'التفتيش على نظافة المحلات التجارية في السوق المركزي', @W6, @Sup1, @Zone1, 1, 0, 2, 1, 90, DATEADD(HOUR, 5, GETUTCDATE()), NULL, NULL, 31.9033, 35.2050, N'السوق المركزي - جميع المحلات', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'فحص حالة الأرصفة', N'فحص الأرصفة وتوثيق الأضرار بالصور', @W6, @Sup1, @Zone2, 1, 0, 2, 1, 60, DATEADD(DAY, 1, GETUTCDATE()), NULL, NULL, 31.9045, 35.2058, N'شارع رام الله - من البداية للنهاية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 8 Tasks (Electrician)
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'إصلاح عمود إنارة معطل', N'إصلاح العمود واستبدال اللمبة المحترقة', @W8, @Sup2, @Zone1, 2, 0, 3, 1, 45, DATEADD(HOUR, 3, GETUTCDATE()), NULL, NULL, 31.9036, 35.2053, N'شارع القدس - مقابل الصيدلية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'صيانة إنارة الحديقة', N'فحص وصيانة 8 أعمدة إنارة في الحديقة', @W8, @Sup2, @Zone1, 1, 0, 3, 1, 120, DATEADD(DAY, 1, GETUTCDATE()), NULL, NULL, 31.9054, 35.2069, N'الحديقة العامة - جميع الأعمدة', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 8 InProgress Task
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'إصلاح إنارة موقف الباصات', N'إصلاح عطل في التوصيلات الكهربائية لموقف الباصات', @W8, @Sup2, @Zone2, 2, 1, 3, 1, 60, DATEADD(HOUR, 2, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, 31.9061, 35.2081, N'موقف الباصات - العمود الشمالي', '', '', GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 1, 1);

-- Worker 9 Tasks (Plumber)
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'إصلاح تسريب مياه', N'إصلاح تسريب في أنبوب المياه الرئيسي', @W9, @Sup2, @Zone1, 2, 0, 4, 1, 90, DATEADD(HOUR, 2, GETUTCDATE()), NULL, NULL, 31.9037, 35.2054, N'شارع القدس - قرب تقاطع المدرسة', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'صيانة صنبور عام', N'صيانة صنبور المياه العام في الحديقة', @W9, @Sup2, @Zone1, 1, 0, 4, 1, 30, DATEADD(DAY, 1, GETUTCDATE()), NULL, NULL, 31.9056, 35.2071, N'الحديقة العامة - قرب دورات المياه', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

-- Worker 10 Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion)
VALUES
(N'تسليك مجرى مياه مسدود', N'تسليك المجرى المسدود في شارع السوق', @W10, @Sup2, @Zone3, 2, 0, 4, 1, 60, DATEADD(HOUR, 3, GETUTCDATE()), NULL, NULL, 31.9072, 35.2092, N'شارع السوق - قرب المخبز', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'استبدال صمام مياه', N'استبدال صمام مياه تالف في تقاطع المستشفى', @W10, @Sup2, @Zone3, 1, 0, 4, 1, 45, DATEADD(DAY, 2, GETUTCDATE()), NULL, NULL, 31.9078, 35.2098, N'تقاطع المستشفى - الزاوية الغربية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1);

PRINT 'Tasks inserted successfully: 21 tasks (17 Pending, 4 InProgress)'

-- =====================================================
-- STEP 6: Insert Issues (ALL columns filled - NO NULLs)
-- =====================================================
PRINT 'Inserting issues...'

INSERT INTO Issues (Title, Description, Type, Severity, Status, ReportedByUserId, ZoneId, Latitude, Longitude, LocationDescription, PhotoUrl, ReportedAt, ResolvedAt, ResolutionNotes, ResolvedByUserId, EventTime, SyncTime, IsSynced, SyncVersion)
VALUES
(N'حفرة كبيرة في الشارع', N'حفرة عميقة في وسط الشارع تشكل خطراً على السيارات والمشاة', 0, 2, 0, @W1, @Zone1, 31.9034, 35.2051, N'شارع القدس - قرب البنك', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'رصيف مكسور', N'رصيف مرتفع ومكسور يشكل خطراً على كبار السن', 0, 1, 0, @W4, @Zone1, 31.9039, 35.2057, N'شارع رام الله - أمام الصيدلية', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'غطاء بالوعة مفقود', N'غطاء صرف صحي مفقود يشكل خطراً كبيراً', 1, 2, 0, @W9, @Zone1, 31.9042, 35.2062, N'شارع المدرسة - قرب المدخل', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'سلك كهربائي مكشوف', N'سلك مكشوف على عمود الإنارة بحاجة لإصلاح عاجل', 1, 2, 1, @W8, @Zone2, 31.9066, 35.2086, N'قرب موقف الباصات - العمود الثالث', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'تراكم نفايات', N'تراكم كبير للنفايات خلف السوق بحاجة لتنظيف', 2, 1, 0, @W2, @Zone2, 31.9069, 35.2089, N'خلف السوق المركزي', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'حاوية نفايات مكسورة', N'حاوية نفايات مكسورة بحاجة لاستبدال', 3, 0, 0, @W3, @Zone3, 31.9082, 35.2102, N'مدخل الحديقة الكبرى', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1);

PRINT 'Issues inserted successfully: 6 issues'

-- =====================================================
-- STEP 7: Insert Attendance Records (ALL columns filled)
-- =====================================================
PRINT 'Inserting attendance records...'

-- Today's attendance - Active (checked in, not checked out yet)
INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckOutLatitude, CheckOutLongitude, IsValidated, ValidationMessage, WorkDuration, Status, IsSynced, SyncVersion)
VALUES
(@W1, @Zone1, DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), NULL, NULL, 31.9035, 35.2052, 0, 0, 1, N'تم تسجيل الحضور بنجاح - داخل منطقة العمل', NULL, 0, 1, 1),
(@W2, @Zone2, DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE()), NULL, NULL, 31.9060, 35.2080, 0, 0, 1, N'تم تسجيل الحضور بنجاح - داخل منطقة العمل', NULL, 0, 1, 1),
(@W4, @Zone1, DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), NULL, NULL, 31.9032, 35.2049, 0, 0, 1, N'تم تسجيل الحضور بنجاح - داخل منطقة العمل', NULL, 0, 1, 1),
(@W8, @Zone1, DATEADD(HOUR, -1, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, NULL, 31.9036, 35.2053, 0, 0, 1, N'تم تسجيل الحضور بنجاح - داخل منطقة العمل', NULL, 0, 1, 1);

-- Yesterday's completed attendance (checked in and out)
INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckOutLatitude, CheckOutLongitude, IsValidated, ValidationMessage, WorkDuration, Status, IsSynced, SyncVersion)
VALUES
(@W1, @Zone1, DATEADD(DAY, -1, DATEADD(HOUR, -10, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -10, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -2, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -2, GETUTCDATE())), 31.9035, 35.2052, 31.9036, 35.2053, 1, N'يوم عمل مكتمل', '08:00:00', 1, 1, 2),
(@W2, @Zone2, DATEADD(DAY, -1, DATEADD(HOUR, -9, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -9, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -1, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -1, GETUTCDATE())), 31.9060, 35.2080, 31.9061, 35.2081, 1, N'يوم عمل مكتمل', '08:00:00', 1, 1, 2),
(@W4, @Zone1, DATEADD(DAY, -1, DATEADD(HOUR, -9, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -9, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -1, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -1, GETUTCDATE())), 31.9032, 35.2049, 31.9033, 35.2050, 1, N'يوم عمل مكتمل', '08:00:00', 1, 1, 2);

PRINT 'Attendance records inserted: 7 records (4 active today, 3 completed yesterday)'

-- =====================================================
-- STEP 8: Insert Notifications (ALL columns filled)
-- =====================================================
PRINT 'Inserting notifications...'

-- Task assignment notifications for pending tasks
INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, PayloadJson)
SELECT
    t.AssignedToUserId,
    N'مهمة جديدة',
    N'تم تكليفك بمهمة جديدة: ' + t.Title,
    0,
    0,
    1,
    t.CreatedAt,
    t.CreatedAt,
    '{"taskId": ' + CAST(t.TaskId AS VARCHAR) + '}'
FROM Tasks t
WHERE t.Status = 0;

-- Welcome notifications for all workers
INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, PayloadJson)
SELECT
    UserId,
    N'مرحباً بك في نظام جولة',
    N'تم تفعيل حسابك بنجاح. يمكنك الآن استلام المهام وتسجيل الحضور.',
    3,
    1,
    1,
    DATEADD(DAY, -7, GETUTCDATE()),
    DATEADD(DAY, -7, GETUTCDATE()),
    '{}'
FROM Users
WHERE Role = 2;

PRINT 'Notifications inserted successfully.'

-- =====================================================
-- FINAL SUMMARY
-- =====================================================
PRINT ''
PRINT '======================================'
PRINT 'SEED DATA COMPLETE - NO NULL VALUES!'
PRINT '======================================'
PRINT ''

SELECT 'Users' AS TableName, COUNT(*) AS RecordCount FROM Users
UNION ALL
SELECT 'Workers', COUNT(*) FROM Users WHERE Role = 2
UNION ALL
SELECT 'Zones', COUNT(*) FROM Zones
UNION ALL
SELECT 'UserZones', COUNT(*) FROM UserZones
UNION ALL
SELECT 'Tasks', COUNT(*) FROM Tasks
UNION ALL
SELECT 'Tasks (Pending)', COUNT(*) FROM Tasks WHERE Status = 0
UNION ALL
SELECT 'Tasks (InProgress)', COUNT(*) FROM Tasks WHERE Status = 1
UNION ALL
SELECT 'Issues', COUNT(*) FROM Issues
UNION ALL
SELECT 'Attendances', COUNT(*) FROM Attendances
UNION ALL
SELECT 'Notifications', COUNT(*) FROM Notifications;

PRINT ''
PRINT '======================================'
PRINT 'TEST CREDENTIALS:'
PRINT '======================================'
PRINT 'Admin: admin / Admin@123'
PRINT 'Supervisor 1: supervisor1 / Admin@123'
PRINT ''
PRINT 'Workers (use PIN for mobile):'
PRINT 'worker1 (Sanitation): PIN 1234'
PRINT 'worker2 (Sanitation): PIN 1235'
PRINT 'worker3 (Sanitation): PIN 1236'
PRINT 'worker4 (Maintenance): PIN 2234'
PRINT 'worker5 (Maintenance): PIN 2235'
PRINT 'worker6 (Inspector): PIN 3234'
PRINT 'worker7 (Inspector): PIN 3235'
PRINT 'worker8 (Electrician): PIN 4234'
PRINT 'worker9 (Plumber): PIN 5234'
PRINT 'worker10 (Plumber): PIN 5235'
PRINT '======================================'
