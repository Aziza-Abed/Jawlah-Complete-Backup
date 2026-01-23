-- =====================================================
-- FollowUp Database - Al-Bireh Municipality Realistic Data
-- Based on actual municipality structure:
-- - صحة/نظافة: 100 workers (by zones, routine tasks)
-- - أشغال: 30 workers (groups of 5)
-- - زراعة: 18 workers (teams of 3-4)
-- - صيانة: General maintenance
-- Password for all users: Admin@123
-- =====================================================
SET QUOTED_IDENTIFIER ON;
GO

-- STEP 1: Clear all existing data (order matters for foreign keys)
-- =====================================================
PRINT 'Clearing existing data...'

DELETE FROM Appeals;
DELETE FROM AuditLogs;
DELETE FROM Photos;
DELETE FROM Notifications;
DELETE FROM LocationHistories;
DELETE FROM Issues;
DELETE FROM Tasks;
DELETE FROM Attendances;
DELETE FROM UserZones;
DELETE FROM Users;
DELETE FROM Teams;
DELETE FROM Departments;

PRINT 'Data cleared successfully.'

-- STEP 2: Reset identity columns
-- =====================================================
DBCC CHECKIDENT ('Users', RESEED, 0);
DBCC CHECKIDENT ('Tasks', RESEED, 0);
DBCC CHECKIDENT ('Issues', RESEED, 0);
DBCC CHECKIDENT ('Attendances', RESEED, 0);
DBCC CHECKIDENT ('Notifications', RESEED, 0);
DBCC CHECKIDENT ('Appeals', RESEED, 0);
DBCC CHECKIDENT ('AuditLogs', RESEED, 0);
DBCC CHECKIDENT ('Departments', RESEED, 0);
DBCC CHECKIDENT ('Teams', RESEED, 0);

-- =====================================================
-- STEP 3: Insert Departments (matching municipality structure)
-- =====================================================
PRINT 'Inserting departments...'

DECLARE @MunicipalityId INT = (SELECT TOP 1 MunicipalityId FROM Municipalities ORDER BY MunicipalityId);

INSERT INTO Departments (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES
(@MunicipalityId, N'قسم الصحة والنظافة', 'Health & Sanitation Department', 'SANITATION', N'قسم الصحة والنظافة - 100 عامل موزعين على المناطق', 1, GETUTCDATE()),
(@MunicipalityId, N'قسم الأشغال', 'Public Works Department', 'PUBLICWORKS', N'قسم الأشغال - 30 عامل في مجموعات من 5', 1, GETUTCDATE()),
(@MunicipalityId, N'قسم الزراعة', 'Agriculture Department', 'AGRICULTURE', N'قسم الزراعة - 18 عامل في فرق من 3-4', 1, GETUTCDATE()),
(@MunicipalityId, N'قسم الصيانة', 'Maintenance Department', 'MAINTENANCE', N'قسم الصيانة العامة', 1, GETUTCDATE());

DECLARE @DeptSanitation INT = (SELECT DepartmentId FROM Departments WHERE Code = 'SANITATION');
DECLARE @DeptPublicWorks INT = (SELECT DepartmentId FROM Departments WHERE Code = 'PUBLICWORKS');
DECLARE @DeptAgriculture INT = (SELECT DepartmentId FROM Departments WHERE Code = 'AGRICULTURE');
DECLARE @DeptMaintenance INT = (SELECT DepartmentId FROM Departments WHERE Code = 'MAINTENANCE');

PRINT 'Departments inserted: 4 departments'

-- =====================================================
-- STEP 4: Insert Teams
-- =====================================================
PRINT 'Inserting teams...'

-- Public Works Teams (6 groups x 5 workers = 30)
INSERT INTO Teams (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES
(@DeptPublicWorks, N'مجموعة الأشغال 1', 'PW-G1', N'مجموعة صيانة الشوارع - المنطقة الشمالية', 5, 1, GETUTCDATE()),
(@DeptPublicWorks, N'مجموعة الأشغال 2', 'PW-G2', N'مجموعة صيانة الشوارع - المنطقة الجنوبية', 5, 1, GETUTCDATE()),
(@DeptPublicWorks, N'مجموعة الأشغال 3', 'PW-G3', N'مجموعة صيانة الأرصفة', 5, 1, GETUTCDATE()),
(@DeptPublicWorks, N'مجموعة الأشغال 4', 'PW-G4', N'مجموعة الإصلاحات الطارئة', 5, 1, GETUTCDATE()),
(@DeptPublicWorks, N'مجموعة الأشغال 5', 'PW-G5', N'مجموعة صيانة البنية التحتية', 5, 1, GETUTCDATE()),
(@DeptPublicWorks, N'مجموعة الأشغال 6', 'PW-G6', N'مجموعة المشاريع الخاصة', 5, 1, GETUTCDATE());

-- Agriculture Teams (5 teams x 3-4 workers = 18)
INSERT INTO Teams (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES
(@DeptAgriculture, N'فريق الزراعة 1', 'AG-T1', N'فريق الحدائق العامة', 4, 1, GETUTCDATE()),
(@DeptAgriculture, N'فريق الزراعة 2', 'AG-T2', N'فريق الأشجار والتشجير', 4, 1, GETUTCDATE()),
(@DeptAgriculture, N'فريق الزراعة 3', 'AG-T3', N'فريق الري والمسطحات الخضراء', 4, 1, GETUTCDATE()),
(@DeptAgriculture, N'فريق الزراعة 4', 'AG-T4', N'فريق الصيانة الزراعية', 3, 1, GETUTCDATE()),
(@DeptAgriculture, N'فريق الزراعة 5', 'AG-T5', N'فريق المشاتل والزهور', 3, 1, GETUTCDATE());

-- Get Team IDs
DECLARE @TeamPW1 INT = (SELECT TeamId FROM Teams WHERE Code = 'PW-G1');
DECLARE @TeamPW2 INT = (SELECT TeamId FROM Teams WHERE Code = 'PW-G2');
DECLARE @TeamPW3 INT = (SELECT TeamId FROM Teams WHERE Code = 'PW-G3');
DECLARE @TeamPW4 INT = (SELECT TeamId FROM Teams WHERE Code = 'PW-G4');
DECLARE @TeamPW5 INT = (SELECT TeamId FROM Teams WHERE Code = 'PW-G5');
DECLARE @TeamPW6 INT = (SELECT TeamId FROM Teams WHERE Code = 'PW-G6');
DECLARE @TeamAG1 INT = (SELECT TeamId FROM Teams WHERE Code = 'AG-T1');
DECLARE @TeamAG2 INT = (SELECT TeamId FROM Teams WHERE Code = 'AG-T2');
DECLARE @TeamAG3 INT = (SELECT TeamId FROM Teams WHERE Code = 'AG-T3');
DECLARE @TeamAG4 INT = (SELECT TeamId FROM Teams WHERE Code = 'AG-T4');
DECLARE @TeamAG5 INT = (SELECT TeamId FROM Teams WHERE Code = 'AG-T5');

PRINT 'Teams inserted: 11 teams (6 Public Works, 5 Agriculture)'

-- =====================================================
-- STEP 5: Insert Users
-- =====================================================
PRINT 'Inserting users...'

-- Password hash for "Admin@123" (ASP.NET Core Identity format)
DECLARE @PasswordHash NVARCHAR(500) = 'AQAAAAEAACcQAAAAEPgxkgOl9DCfl3J8M0L0T603OaLiLCtKJXv2L7yn2crkrU2IwQBTRaQMh5Xbw3vO7Q==';

-- ============ ADMIN ============
INSERT INTO Users (Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, Status, CreatedAt, LastLoginAt, FcmToken, MunicipalityId, ExpectedStartTime, ExpectedEndTime)
VALUES
('admin', @PasswordHash, 'admin@albireh.ps', '+970599000001', N'مدير النظام', 0, NULL, NULL, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00');

-- ============ SUPERVISORS (مراقبين) ============
INSERT INTO Users (Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, Status, CreatedAt, LastLoginAt, FcmToken, MunicipalityId, ExpectedStartTime, ExpectedEndTime)
VALUES
-- Sanitation Supervisors (2 - one for north, one for south)
('sup.sanitation1', @PasswordHash, 'khalid.supervisor@albireh.ps', '+970599100001', N'خالد أبو سعدة', 1, NULL, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('sup.sanitation2', @PasswordHash, 'ahmad.alqadi@albireh.ps', '+970599100002', N'أحمد القاضي', 1, NULL, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),

-- Public Works Supervisors (2)
('sup.publicworks1', @PasswordHash, 'mahmoud.hasan@albireh.ps', '+970599100003', N'محمود حسن الشريف', 1, NULL, @DeptPublicWorks, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('sup.publicworks2', @PasswordHash, 'samer.khalil@albireh.ps', '+970599100004', N'سامر خليل عودة', 1, NULL, @DeptPublicWorks, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),

-- Agriculture Supervisor (1)
('sup.agriculture', @PasswordHash, 'naser.saleh@albireh.ps', '+970599100005', N'ناصر صالح الحاج', 1, NULL, @DeptAgriculture, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),

-- Maintenance Supervisor (1)
('sup.maintenance', @PasswordHash, 'fadi.omar@albireh.ps', '+970599100006', N'فادي عمر زيدان', 1, NULL, @DeptMaintenance, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00');

-- ============ SANITATION WORKERS (صحة/نظافة) - Sample of 15 ============
INSERT INTO Users (Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, Status, CreatedAt, LastLoginAt, FcmToken, MunicipalityId, ExpectedStartTime, ExpectedEndTime)
VALUES
('san.worker1', @PasswordHash, NULL, '+970593001001', N'أحمد حسن محمود', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker2', @PasswordHash, NULL, '+970593001002', N'محمد علي عبدالله', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker3', @PasswordHash, NULL, '+970593001003', N'عمر خالد ياسين', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker4', @PasswordHash, NULL, '+970593001004', N'يوسف أحمد سليم', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker5', @PasswordHash, NULL, '+970593001005', N'إبراهيم محمد نصر', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker6', @PasswordHash, NULL, '+970593001006', N'حسين علي داود', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker7', @PasswordHash, NULL, '+970593001007', N'رائد سمير الخطيب', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker8', @PasswordHash, NULL, '+970593001008', N'ماجد حمدان عيسى', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker9', @PasswordHash, NULL, '+970593001009', N'وليد صبحي مصطفى', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker10', @PasswordHash, NULL, '+970593001010', N'طارق جمال الدين', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '06:00:00', '14:00:00'),
('san.worker11', @PasswordHash, NULL, '+970593001011', N'فراس عادل حسين', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '14:00:00', '22:00:00'),
('san.worker12', @PasswordHash, NULL, '+970593001012', N'باسم نبيل شاهين', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '14:00:00', '22:00:00'),
('san.worker13', @PasswordHash, NULL, '+970593001013', N'نضال كريم العمري', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '14:00:00', '22:00:00'),
('san.worker14', @PasswordHash, NULL, '+970593001014', N'هاني رشيد الجابري', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '14:00:00', '22:00:00'),
('san.worker15', @PasswordHash, NULL, '+970593001015', N'زياد فؤاد النجار', 2, 0, @DeptSanitation, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '14:00:00', '22:00:00');

-- ============ PUBLIC WORKS WORKERS (أشغال) - 15 workers in groups ============
INSERT INTO Users (Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, Status, CreatedAt, LastLoginAt, FcmToken, MunicipalityId, ExpectedStartTime, ExpectedEndTime)
VALUES
-- Group 1
('pw.worker1', @PasswordHash, NULL, '+970593002001', N'سامي ناصر حمدان', 2, 1, @DeptPublicWorks, @TeamPW1, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker2', @PasswordHash, NULL, '+970593002002', N'فادي محمود سليم', 2, 1, @DeptPublicWorks, @TeamPW1, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker3', @PasswordHash, NULL, '+970593002003', N'رامي صالح عودة', 2, 1, @DeptPublicWorks, @TeamPW1, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
-- Group 2
('pw.worker4', @PasswordHash, NULL, '+970593002004', N'عماد حسين الطويل', 2, 1, @DeptPublicWorks, @TeamPW2, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker5', @PasswordHash, NULL, '+970593002005', N'ياسر عبد الكريم', 2, 1, @DeptPublicWorks, @TeamPW2, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker6', @PasswordHash, NULL, '+970593002006', N'أشرف ماهر الحسيني', 2, 1, @DeptPublicWorks, @TeamPW2, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
-- Group 3
('pw.worker7', @PasswordHash, NULL, '+970593002007', N'منير سعيد بركات', 2, 1, @DeptPublicWorks, @TeamPW3, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker8', @PasswordHash, NULL, '+970593002008', N'جمال راغب عيد', 2, 1, @DeptPublicWorks, @TeamPW3, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker9', @PasswordHash, NULL, '+970593002009', N'خالد فايز القدومي', 2, 1, @DeptPublicWorks, @TeamPW3, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
-- Group 4
('pw.worker10', @PasswordHash, NULL, '+970593002010', N'نادر حافظ شحادة', 2, 1, @DeptPublicWorks, @TeamPW4, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker11', @PasswordHash, NULL, '+970593002011', N'بسام عدنان الرفاعي', 2, 1, @DeptPublicWorks, @TeamPW4, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker12', @PasswordHash, NULL, '+970593002012', N'شادي رمزي الناطور', 2, 1, @DeptPublicWorks, @TeamPW4, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
-- Group 5
('pw.worker13', @PasswordHash, NULL, '+970593002013', N'هشام وليد الغول', 2, 1, @DeptPublicWorks, @TeamPW5, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker14', @PasswordHash, NULL, '+970593002014', N'مروان صبري الدجاني', 2, 1, @DeptPublicWorks, @TeamPW5, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('pw.worker15', @PasswordHash, NULL, '+970593002015', N'غسان كمال البرغوثي', 2, 1, @DeptPublicWorks, @TeamPW5, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00');

-- ============ AGRICULTURE WORKERS (زراعة) - 10 workers in teams ============
INSERT INTO Users (Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, Status, CreatedAt, LastLoginAt, FcmToken, MunicipalityId, ExpectedStartTime, ExpectedEndTime)
VALUES
-- Team 1 (4 workers)
('ag.worker1', @PasswordHash, NULL, '+970593003001', N'حازم سليم الزعبي', 2, 2, @DeptAgriculture, @TeamAG1, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker2', @PasswordHash, NULL, '+970593003002', N'نور الدين أحمد', 2, 2, @DeptAgriculture, @TeamAG1, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker3', @PasswordHash, NULL, '+970593003003', N'أيمن خضر جبارين', 2, 2, @DeptAgriculture, @TeamAG1, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker4', @PasswordHash, NULL, '+970593003004', N'ثامر محسن الكيلاني', 2, 2, @DeptAgriculture, @TeamAG1, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
-- Team 2 (3 workers)
('ag.worker5', @PasswordHash, NULL, '+970593003005', N'صهيب عارف النابلسي', 2, 2, @DeptAgriculture, @TeamAG2, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker6', @PasswordHash, NULL, '+970593003006', N'لؤي غازي الصالحي', 2, 2, @DeptAgriculture, @TeamAG2, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker7', @PasswordHash, NULL, '+970593003007', N'راتب هاشم القواسمي', 2, 2, @DeptAgriculture, @TeamAG2, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
-- Team 3 (3 workers)
('ag.worker8', @PasswordHash, NULL, '+970593003008', N'وسام فهد الأعرج', 2, 2, @DeptAgriculture, @TeamAG3, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker9', @PasswordHash, NULL, '+970593003009', N'حسام طلال الجعبري', 2, 2, @DeptAgriculture, @TeamAG3, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('ag.worker10', @PasswordHash, NULL, '+970593003010', N'معتز نعيم الشرباتي', 2, 2, @DeptAgriculture, @TeamAG3, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00');

-- ============ MAINTENANCE WORKERS (صيانة) - 5 workers ============
INSERT INTO Users (Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, Status, CreatedAt, LastLoginAt, FcmToken, MunicipalityId, ExpectedStartTime, ExpectedEndTime)
VALUES
('mnt.worker1', @PasswordHash, NULL, '+970593004001', N'يزن عمر الحاج', 2, 3, @DeptMaintenance, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('mnt.worker2', @PasswordHash, NULL, '+970593004002', N'بلال حسن مصطفى', 2, 3, @DeptMaintenance, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('mnt.worker3', @PasswordHash, NULL, '+970593004003', N'مجدي خليل داود', 2, 3, @DeptMaintenance, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -1, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('mnt.worker4', @PasswordHash, NULL, '+970593004004', N'عصام جبر الخالدي', 2, 3, @DeptMaintenance, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00'),
('mnt.worker5', @PasswordHash, NULL, '+970593004005', N'كامل زهير السعدي', 2, 3, @DeptMaintenance, NULL, 0, GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), '', @MunicipalityId, '07:00:00', '15:00:00');

PRINT 'Users inserted: 1 Admin, 6 Supervisors, 45 Workers'

-- =====================================================
-- STEP 6: Get User IDs for references
-- =====================================================
DECLARE @AdminId INT = (SELECT UserId FROM Users WHERE Username = 'admin');
DECLARE @SupSan1 INT = (SELECT UserId FROM Users WHERE Username = 'sup.sanitation1');
DECLARE @SupSan2 INT = (SELECT UserId FROM Users WHERE Username = 'sup.sanitation2');
DECLARE @SupPW1 INT = (SELECT UserId FROM Users WHERE Username = 'sup.publicworks1');
DECLARE @SupPW2 INT = (SELECT UserId FROM Users WHERE Username = 'sup.publicworks2');
DECLARE @SupAG INT = (SELECT UserId FROM Users WHERE Username = 'sup.agriculture');
DECLARE @SupMNT INT = (SELECT UserId FROM Users WHERE Username = 'sup.maintenance');

-- =====================================================
-- STEP 6b: Assign Workers to Supervisors
-- =====================================================
PRINT 'Assigning workers to supervisors...'

-- Sanitation workers: split between sup.sanitation1 (workers 1-8) and sup.sanitation2 (workers 9-15)
UPDATE Users SET SupervisorId = @SupSan1 WHERE Username IN ('san.worker1', 'san.worker2', 'san.worker3', 'san.worker4', 'san.worker5', 'san.worker6', 'san.worker7', 'san.worker8');
UPDATE Users SET SupervisorId = @SupSan2 WHERE Username IN ('san.worker9', 'san.worker10', 'san.worker11', 'san.worker12', 'san.worker13', 'san.worker14', 'san.worker15');

-- Public Works workers: split between sup.publicworks1 (workers 1-8) and sup.publicworks2 (workers 9-15)
UPDATE Users SET SupervisorId = @SupPW1 WHERE Username IN ('pw.worker1', 'pw.worker2', 'pw.worker3', 'pw.worker4', 'pw.worker5', 'pw.worker6', 'pw.worker7', 'pw.worker8');
UPDATE Users SET SupervisorId = @SupPW2 WHERE Username IN ('pw.worker9', 'pw.worker10', 'pw.worker11', 'pw.worker12', 'pw.worker13', 'pw.worker14', 'pw.worker15');

-- Agriculture workers: all under sup.agriculture
UPDATE Users SET SupervisorId = @SupAG WHERE Username LIKE 'ag.worker%';

-- Maintenance workers: all under sup.maintenance
UPDATE Users SET SupervisorId = @SupMNT WHERE Username LIKE 'mnt.worker%';

PRINT 'Supervisor assignments completed.'

-- Get Zone IDs (should exist from GIS import)
DECLARE @Zone1 INT = (SELECT TOP 1 ZoneId FROM Zones ORDER BY ZoneId);
DECLARE @Zone2 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE ZoneId > @Zone1 ORDER BY ZoneId);
DECLARE @Zone3 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE ZoneId > @Zone2 ORDER BY ZoneId);
DECLARE @Zone4 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE ZoneId > @Zone3 ORDER BY ZoneId);
DECLARE @Zone5 INT = (SELECT TOP 1 ZoneId FROM Zones WHERE ZoneId > @Zone4 ORDER BY ZoneId);

-- =====================================================
-- STEP 7: Assign Workers to Zones
-- =====================================================
PRINT 'Assigning workers to zones...'

-- Assign Sanitation workers to zones (they work by zone)
INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId,
       CASE
           WHEN u.Username IN ('san.worker1', 'san.worker2', 'san.worker3') THEN @Zone1
           WHEN u.Username IN ('san.worker4', 'san.worker5', 'san.worker6') THEN @Zone2
           WHEN u.Username IN ('san.worker7', 'san.worker8', 'san.worker9') THEN @Zone3
           WHEN u.Username IN ('san.worker10', 'san.worker11', 'san.worker12') THEN @Zone4
           ELSE @Zone5
       END,
       @AdminId, GETUTCDATE(), 1
FROM Users u
WHERE u.Username LIKE 'san.worker%' AND @Zone1 IS NOT NULL;

-- Assign other workers to zones (less strict - they move around)
INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1
FROM Users u WHERE u.Username LIKE 'pw.worker%' AND @Zone1 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone2, @AdminId, GETUTCDATE(), 1
FROM Users u WHERE u.Username LIKE 'ag.worker%' AND @Zone2 IS NOT NULL;

INSERT INTO UserZones (UserId, ZoneId, AssignedByUserId, AssignedAt, IsActive)
SELECT u.UserId, @Zone1, @AdminId, GETUTCDATE(), 1
FROM Users u WHERE u.Username LIKE 'mnt.worker%' AND @Zone1 IS NOT NULL;

PRINT 'Zone assignments completed.'

-- =====================================================
-- STEP 8: Insert Tasks (~80 tasks)
-- =====================================================
PRINT 'Inserting tasks...'

-- Get worker IDs
DECLARE @SanW1 INT = (SELECT UserId FROM Users WHERE Username = 'san.worker1');
DECLARE @SanW2 INT = (SELECT UserId FROM Users WHERE Username = 'san.worker2');
DECLARE @SanW3 INT = (SELECT UserId FROM Users WHERE Username = 'san.worker3');
DECLARE @SanW4 INT = (SELECT UserId FROM Users WHERE Username = 'san.worker4');
DECLARE @SanW5 INT = (SELECT UserId FROM Users WHERE Username = 'san.worker5');
DECLARE @PWW1 INT = (SELECT UserId FROM Users WHERE Username = 'pw.worker1');
DECLARE @PWW2 INT = (SELECT UserId FROM Users WHERE Username = 'pw.worker2');
DECLARE @PWW3 INT = (SELECT UserId FROM Users WHERE Username = 'pw.worker3');
DECLARE @PWW4 INT = (SELECT UserId FROM Users WHERE Username = 'pw.worker4');
DECLARE @AGW1 INT = (SELECT UserId FROM Users WHERE Username = 'ag.worker1');
DECLARE @AGW2 INT = (SELECT UserId FROM Users WHERE Username = 'ag.worker2');
DECLARE @AGW3 INT = (SELECT UserId FROM Users WHERE Username = 'ag.worker3');
DECLARE @MNTW1 INT = (SELECT UserId FROM Users WHERE Username = 'mnt.worker1');
DECLARE @MNTW2 INT = (SELECT UserId FROM Users WHERE Username = 'mnt.worker2');

-- ============ SANITATION TASKS (روتينية - يومية) ============
-- Status: 0=Pending, 1=InProgress, 2=Completed, 4=Approved, 5=Rejected

-- Pending Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
(N'تنظيف شارع القدس الرئيسي', N'تنظيف شامل للشارع وإزالة النفايات من الأرصفة', @SanW1, @SupSan1, @Zone1, 2, 0, 1, 1, 60, DATEADD(HOUR, 4, GETUTCDATE()), NULL, NULL, 31.9035, 35.2052, N'شارع القدس - من التقاطع حتى الدوار', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'جمع النفايات - حي الشمال', N'جمع النفايات من 15 حاوية في حي الشمال', @SanW1, @SupSan1, @Zone1, 2, 0, 0, 1, 90, DATEADD(HOUR, 6, GETUTCDATE()), NULL, NULL, 31.9041, 35.2061, N'حي الشمال - جميع الحاويات', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'تنظيف ساحة البلدية', N'تنظيف الساحة العامة أمام مبنى البلدية', @SanW2, @SupSan1, @Zone1, 1, 0, 4, 1, 45, DATEADD(DAY, 1, GETUTCDATE()), NULL, NULL, 31.9022, 35.2044, N'ساحة البلدية الرئيسية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'تنظيف موقف الباصات', N'تنظيف موقف الباصات المركزي', @SanW3, @SupSan1, @Zone2, 2, 0, 1, 1, 50, DATEADD(HOUR, 3, GETUTCDATE()), NULL, NULL, 31.9060, 35.2080, N'موقف الباصات المركزي', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'تنظيف محيط المدرسة', N'تنظيف الشوارع المحيطة بالمدرسة', @SanW4, @SupSan2, @Zone2, 2, 0, 1, 1, 45, DATEADD(HOUR, 2, GETUTCDATE()), NULL, NULL, 31.9065, 35.2085, N'مدرسة البيرة الثانوية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'جمع النفايات - السوق', N'جمع النفايات من محيط السوق المركزي', @SanW5, @SupSan2, @Zone3, 2, 0, 0, 1, 60, DATEADD(HOUR, 5, GETUTCDATE()), NULL, NULL, 31.9080, 35.2100, N'السوق المركزي', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0);

-- InProgress Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
(N'تنظيف حديقة الأطفال', N'تنظيف الحديقة العامة وجمع الأوراق المتساقطة', @SanW1, @SupSan1, @Zone1, 1, 1, 4, 1, 40, DATEADD(HOUR, 2, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, 31.9055, 35.2070, N'حديقة الأطفال - المدخل الشرقي', '', '', GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), GETUTCDATE(), 1, 1, 50),
(N'تنظيف الشارع الجديد', N'كنس وتنظيف الشارع الجديد', @SanW2, @SupSan1, @Zone1, 1, 1, 1, 1, 30, DATEADD(HOUR, 1, GETUTCDATE()), DATEADD(MINUTE, -30, GETUTCDATE()), NULL, 31.9048, 35.2058, N'الشارع الجديد - من البداية للنهاية', '', '', GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 1, 1, 70);

-- Completed Tasks (awaiting approval)
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
(N'تنظيف شارع رام الله', N'تنظيف شامل لشارع رام الله الرئيسي', @SanW3, @SupSan1, @Zone2, 2, 2, 1, 1, 60, DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -4, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), 31.9045, 35.2058, N'شارع رام الله الرئيسي', N'تم التنظيف بنجاح', '', DATEADD(HOUR, -1, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE()), GETUTCDATE(), 1, 2, 100),
(N'جمع النفايات - حي الجنوب', N'جمع النفايات من حاويات حي الجنوب', @SanW4, @SupSan2, @Zone3, 2, 2, 0, 1, 90, DATEADD(HOUR, -1, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE()), DATEADD(MINUTE, -30, GETUTCDATE()), 31.9030, 35.2040, N'حي الجنوب - 12 حاوية', N'تم جمع جميع النفايات', '', DATEADD(MINUTE, -30, GETUTCDATE()), DATEADD(HOUR, -4, GETUTCDATE()), GETUTCDATE(), 1, 2, 100);

-- Approved Tasks
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
(N'تنظيف ساحة المسجد', N'تنظيف ساحة المسجد الكبير', @SanW1, @SupSan1, @Zone1, 2, 4, 4, 1, 45, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, DATEADD(HOUR, -5, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -3, GETUTCDATE())), 31.9038, 35.2055, N'ساحة المسجد الكبير', N'تم التنظيف بشكل ممتاز', '', DATEADD(DAY, -1, DATEADD(HOUR, -3, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -6, GETUTCDATE())), DATEADD(DAY, -1, GETUTCDATE()), 1, 3, 100),
(N'تنظيف شارع المدرسة', N'تنظيف شارع المدرسة قبل الدوام', @SanW2, @SupSan1, @Zone1, 2, 4, 1, 1, 30, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, DATEADD(HOUR, -8, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -7, GETUTCDATE())), 31.9042, 35.2062, N'شارع المدرسة', N'تم إنجاز المهمة', '', DATEADD(DAY, -1, DATEADD(HOUR, -7, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -9, GETUTCDATE())), DATEADD(DAY, -1, GETUTCDATE()), 1, 3, 100);

-- ============ PUBLIC WORKS TASKS (أشغال) ============
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
-- Pending
(N'إصلاح رصيف مكسور', N'إصلاح الرصيف المكسور أمام البنك العربي', @PWW1, @SupPW1, @Zone1, 2, 0, 3, 1, 90, DATEADD(HOUR, 6, GETUTCDATE()), NULL, NULL, 31.9032, 35.2049, N'أمام البنك العربي - شارع القدس', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'صيانة مقاعد الحديقة', N'إصلاح 5 مقاعد خشبية تالفة', @PWW2, @SupPW1, @Zone1, 1, 0, 3, 1, 120, DATEADD(DAY, 2, GETUTCDATE()), NULL, NULL, 31.9055, 35.2070, N'حديقة الأطفال - منطقة الجلوس', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'إصلاح سياج حديدي', N'إصلاح السياج الحديدي المائل', @PWW3, @SupPW1, @Zone2, 2, 0, 3, 1, 60, DATEADD(HOUR, 4, GETUTCDATE()), NULL, NULL, 31.9062, 35.2082, N'شارع الإرسال - مقابل المسجد', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'ترميم جدار متصدع', N'إصلاح تصدعات في جدار الحديقة', @PWW4, @SupPW2, @Zone2, 1, 0, 3, 1, 180, DATEADD(DAY, 3, GETUTCDATE()), NULL, NULL, 31.9068, 35.2088, N'جدار الحديقة الشرقي', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),

-- InProgress
(N'إصلاح باب حديقة', N'إصلاح مفصلات باب الحديقة الرئيسي', @PWW1, @SupPW1, @Zone1, 1, 1, 3, 1, 30, DATEADD(HOUR, 3, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, 31.9052, 35.2068, N'مدخل حديقة الأطفال', '', '', GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 1, 1, 60),
(N'طلاء سور المدرسة', N'إعادة طلاء السور الخارجي للمدرسة', @PWW2, @SupPW1, @Zone1, 1, 1, 3, 1, 240, DATEADD(DAY, 1, GETUTCDATE()), DATEADD(HOUR, -3, GETUTCDATE()), NULL, 31.9065, 35.2085, N'سور مدرسة البيرة', '', '', GETUTCDATE(), DATEADD(HOUR, -4, GETUTCDATE()), GETUTCDATE(), 1, 1, 30),

-- Completed
(N'إصلاح درج عام', N'إصلاح درج مكسور في الساحة العامة', @PWW3, @SupPW1, @Zone2, 2, 2, 3, 1, 45, DATEADD(HOUR, -2, GETUTCDATE()), DATEADD(HOUR, -4, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), 31.9058, 35.2078, N'الساحة العامة - الدرج الشرقي', N'تم إصلاح الدرج واستبدال البلاط', '', DATEADD(HOUR, -1, GETUTCDATE()), DATEADD(HOUR, -5, GETUTCDATE()), GETUTCDATE(), 1, 2, 100),

-- Approved
(N'صيانة أعمدة إنارة', N'صيانة وتنظيف 10 أعمدة إنارة', @PWW4, @SupPW2, @Zone3, 1, 4, 3, 1, 120, DATEADD(DAY, -2, GETUTCDATE()), DATEADD(DAY, -2, DATEADD(HOUR, -6, GETUTCDATE())), DATEADD(DAY, -2, DATEADD(HOUR, -4, GETUTCDATE())), 31.9075, 35.2095, N'شارع الوحدة - 10 أعمدة', N'تم الصيانة بنجاح', '', DATEADD(DAY, -2, DATEADD(HOUR, -4, GETUTCDATE())), DATEADD(DAY, -2, DATEADD(HOUR, -7, GETUTCDATE())), DATEADD(DAY, -2, GETUTCDATE()), 1, 3, 100);

-- ============ AGRICULTURE TASKS (زراعة) ============
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
-- Pending
(N'تقليم أشجار الحديقة', N'تقليم وتشذيب أشجار الحديقة الكبرى', @AGW1, @SupAG, @Zone2, 1, 0, 4, 1, 180, DATEADD(DAY, 2, GETUTCDATE()), NULL, NULL, 31.9058, 35.2078, N'الحديقة الكبرى - الأشجار الغربية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'زراعة أزهار موسمية', N'زراعة أزهار الربيع في أحواض الحديقة', @AGW2, @SupAG, @Zone2, 1, 0, 4, 1, 120, DATEADD(DAY, 3, GETUTCDATE()), NULL, NULL, 31.9060, 35.2080, N'أحواض الحديقة الرئيسية', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'ري المسطحات الخضراء', N'ري المسطحات الخضراء في الحديقة', @AGW3, @SupAG, @Zone2, 2, 0, 4, 1, 60, DATEADD(HOUR, 4, GETUTCDATE()), NULL, NULL, 31.9055, 35.2075, N'المسطحات الخضراء - الحديقة', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),

-- InProgress
(N'صيانة نظام الري', N'فحص وصيانة نظام الري بالتنقيط', @AGW1, @SupAG, @Zone2, 2, 1, 4, 1, 90, DATEADD(HOUR, 3, GETUTCDATE()), DATEADD(HOUR, -2, GETUTCDATE()), NULL, 31.9062, 35.2082, N'نظام الري - القسم الشمالي', '', '', GETUTCDATE(), DATEADD(HOUR, -3, GETUTCDATE()), GETUTCDATE(), 1, 1, 40),

-- Approved
(N'تسميد الأشجار', N'تسميد أشجار الفاكهة في الحديقة', @AGW2, @SupAG, @Zone2, 1, 4, 4, 1, 60, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, DATEADD(HOUR, -5, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -4, GETUTCDATE())), 31.9065, 35.2085, N'أشجار الفاكهة', N'تم التسميد بنجاح', '', DATEADD(DAY, -1, DATEADD(HOUR, -4, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -6, GETUTCDATE())), DATEADD(DAY, -1, GETUTCDATE()), 1, 3, 100);

-- ============ MAINTENANCE TASKS (صيانة) ============
INSERT INTO Tasks (Title, Description, AssignedToUserId, AssignedByUserId, ZoneId, Priority, Status, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, DueDate, StartedAt, CompletedAt, Latitude, Longitude, LocationDescription, CompletionNotes, PhotoUrl, EventTime, CreatedAt, SyncTime, IsSynced, SyncVersion, ProgressPercentage)
VALUES
-- Pending
(N'صيانة مضخة مياه', N'فحص وصيانة مضخة مياه الحديقة', @MNTW1, @SupMNT, @Zone1, 2, 0, 3, 1, 60, DATEADD(HOUR, 5, GETUTCDATE()), NULL, NULL, 31.9050, 35.2065, N'غرفة المضخات - الحديقة', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),
(N'إصلاح تسريب مياه', N'إصلاح تسريب في خط المياه الرئيسي', @MNTW2, @SupMNT, @Zone1, 3, 0, 3, 1, 90, DATEADD(HOUR, 3, GETUTCDATE()), NULL, NULL, 31.9037, 35.2054, N'شارع القدس - قرب التقاطع', '', '', GETUTCDATE(), GETUTCDATE(), GETUTCDATE(), 1, 1, 0),

-- InProgress
(N'صيانة كهرباء الحديقة', N'فحص وإصلاح توصيلات الكهرباء', @MNTW1, @SupMNT, @Zone1, 2, 1, 3, 1, 45, DATEADD(HOUR, 2, GETUTCDATE()), DATEADD(HOUR, -1, GETUTCDATE()), NULL, 31.9054, 35.2069, N'صندوق الكهرباء - الحديقة', '', '', GETUTCDATE(), DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 1, 1, 70),

-- Approved
(N'استبدال صمام مياه', N'استبدال صمام مياه تالف', @MNTW2, @SupMNT, @Zone1, 2, 4, 3, 1, 30, DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, DATEADD(HOUR, -4, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -3, GETUTCDATE())), 31.9040, 35.2057, N'تقاطع شارع القدس', N'تم الاستبدال بنجاح', '', DATEADD(DAY, -1, DATEADD(HOUR, -3, GETUTCDATE())), DATEADD(DAY, -1, DATEADD(HOUR, -5, GETUTCDATE())), DATEADD(DAY, -1, GETUTCDATE()), 1, 3, 100);

PRINT 'Tasks inserted: ~30 tasks (various statuses)'

-- =====================================================
-- STEP 9: Insert Issues (~20 issues)
-- =====================================================
PRINT 'Inserting issues...'

INSERT INTO Issues (Title, Description, Type, Severity, Status, ReportedByUserId, ZoneId, Latitude, Longitude, LocationDescription, PhotoUrl, ReportedAt, ResolvedAt, ResolutionNotes, ResolvedByUserId, EventTime, SyncTime, IsSynced, SyncVersion)
VALUES
-- Reported (Status = 1)
(N'حفرة كبيرة في الشارع', N'حفرة عميقة في وسط الشارع تشكل خطراً على السيارات', 1, 3, 1, @SanW1, @Zone1, 31.9034, 35.2051, N'شارع القدس - قرب البنك', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'رصيف مكسور', N'رصيف مرتفع ومكسور يشكل خطراً', 1, 2, 1, @PWW1, @Zone1, 31.9039, 35.2057, N'شارع رام الله - أمام الصيدلية', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'غطاء بالوعة مفقود', N'غطاء صرف صحي مفقود يشكل خطراً كبيراً', 2, 4, 1, @MNTW1, @Zone1, 31.9042, 35.2062, N'شارع المدرسة - قرب المدخل', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'تراكم نفايات', N'تراكم كبير للنفايات خلف السوق', 3, 2, 1, @SanW2, @Zone2, 31.9069, 35.2089, N'خلف السوق المركزي', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'شجرة مائلة', N'شجرة كبيرة مائلة قد تسقط', 2, 3, 1, @AGW1, @Zone2, 31.9058, 35.2078, N'الحديقة الكبرى - الزاوية الشمالية', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'حاوية نفايات مكسورة', N'حاوية نفايات مكسورة بحاجة لاستبدال', 4, 1, 1, @SanW3, @Zone3, 31.9082, 35.2102, N'مدخل الحديقة الكبرى', '', GETUTCDATE(), NULL, '', NULL, GETUTCDATE(), GETUTCDATE(), 1, 1),
(N'إنارة معطلة', N'عمود إنارة معطل في الشارع', 1, 2, 1, @MNTW2, @Zone1, 31.9036, 35.2053, N'شارع القدس - مقابل الصيدلية', '', DATEADD(HOUR, -2, GETUTCDATE()), NULL, '', NULL, DATEADD(HOUR, -2, GETUTCDATE()), GETUTCDATE(), 1, 1),
(N'تسريب مياه صغير', N'تسريب مياه من أنبوب جانبي', 1, 1, 1, @SanW4, @Zone2, 31.9065, 35.2085, N'قرب مدرسة البيرة', '', DATEADD(HOUR, -3, GETUTCDATE()), NULL, '', NULL, DATEADD(HOUR, -3, GETUTCDATE()), GETUTCDATE(), 1, 1),

-- Under Review (Status = 2)
(N'طريق ضيق بسبب سيارات', N'سيارات متوقفة تعيق حركة المرور', 5, 2, 2, @SanW1, @Zone1, 31.9040, 35.2060, N'شارع السوق الرئيسي', '', DATEADD(DAY, -1, GETUTCDATE()), NULL, '', NULL, DATEADD(DAY, -1, GETUTCDATE()), GETUTCDATE(), 1, 1),
(N'روائح كريهة', N'روائح كريهة من حاوية نفايات', 3, 2, 2, @SanW2, @Zone2, 31.9070, 35.2090, N'خلف المطعم الكبير', '', DATEADD(DAY, -1, GETUTCDATE()), NULL, '', NULL, DATEADD(DAY, -1, GETUTCDATE()), GETUTCDATE(), 1, 1),

-- Resolved (Status = 3)
(N'أعشاب طويلة', N'أعشاب طويلة تحتاج لقص', 3, 1, 3, @AGW2, @Zone2, 31.9062, 35.2082, N'جانب الحديقة الغربي', '', DATEADD(DAY, -3, GETUTCDATE()), DATEADD(DAY, -2, GETUTCDATE()), N'تم قص الأعشاب', @SupAG, DATEADD(DAY, -2, GETUTCDATE()), GETUTCDATE(), 1, 2),
(N'مقعد مكسور', N'مقعد في الحديقة مكسور', 4, 1, 3, @PWW2, @Zone1, 31.9055, 35.2070, N'حديقة الأطفال', '', DATEADD(DAY, -4, GETUTCDATE()), DATEADD(DAY, -3, GETUTCDATE()), N'تم إصلاح المقعد', @SupPW1, DATEADD(DAY, -3, GETUTCDATE()), GETUTCDATE(), 1, 2),
(N'حفرة صغيرة', N'حفرة صغيرة في الرصيف', 1, 1, 3, @SanW5, @Zone3, 31.9078, 35.2098, N'قرب المستشفى', '', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -4, GETUTCDATE()), N'تم ردم الحفرة', @SupPW2, DATEADD(DAY, -4, GETUTCDATE()), GETUTCDATE(), 1, 2);

PRINT 'Issues inserted: ~13 issues'

-- =====================================================
-- STEP 10: Insert Attendance Records (Last 5 days)
-- =====================================================
PRINT 'Inserting attendance records...'

-- Get all worker IDs
DECLARE @WorkerIds TABLE (UserId INT);
INSERT INTO @WorkerIds SELECT UserId FROM Users WHERE Role = 2;

-- Today's attendance - Active (checked in, not checked out yet)
INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckOutLatitude, CheckOutLongitude, IsValidated, ValidationMessage, WorkDuration, Status, IsSynced, SyncVersion)
SELECT TOP 20
    UserId,
    @Zone1,
    DATEADD(HOUR, -ABS(CHECKSUM(NEWID())) % 4 - 1, GETUTCDATE()),
    DATEADD(HOUR, -ABS(CHECKSUM(NEWID())) % 4 - 1, GETUTCDATE()),
    NULL, NULL,
    31.9035 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    35.2052 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    0, 0,
    1,
    N'تم تسجيل الحضور بنجاح',
    NULL,
    1, -- CheckedIn
    1, 1
FROM @WorkerIds
ORDER BY NEWID();

-- Yesterday's completed attendance
INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckOutLatitude, CheckOutLongitude, IsValidated, ValidationMessage, WorkDuration, Status, IsSynced, SyncVersion)
SELECT
    UserId,
    @Zone1,
    DATEADD(DAY, -1, DATEADD(HOUR, -10, GETUTCDATE())),
    DATEADD(DAY, -1, DATEADD(HOUR, -10, GETUTCDATE())),
    DATEADD(DAY, -1, DATEADD(HOUR, -2, GETUTCDATE())),
    DATEADD(DAY, -1, DATEADD(HOUR, -2, GETUTCDATE())),
    31.9035 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    35.2052 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    31.9036 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    35.2053 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    1,
    N'يوم عمل مكتمل',
    '08:00:00',
    2, -- CheckedOut
    1, 2
FROM @WorkerIds;

-- 2 days ago
INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckOutLatitude, CheckOutLongitude, IsValidated, ValidationMessage, WorkDuration, Status, IsSynced, SyncVersion)
SELECT
    UserId,
    @Zone1,
    DATEADD(DAY, -2, DATEADD(HOUR, -10, GETUTCDATE())),
    DATEADD(DAY, -2, DATEADD(HOUR, -10, GETUTCDATE())),
    DATEADD(DAY, -2, DATEADD(HOUR, -2, GETUTCDATE())),
    DATEADD(DAY, -2, DATEADD(HOUR, -2, GETUTCDATE())),
    31.9035 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    35.2052 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    31.9036 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    35.2053 + (ABS(CHECKSUM(NEWID())) % 100) * 0.0001,
    1,
    N'يوم عمل مكتمل',
    '08:00:00',
    2,
    1, 2
FROM @WorkerIds;

PRINT 'Attendance records inserted for last 3 days'

-- =====================================================
-- STEP 11: Insert Notifications
-- =====================================================
PRINT 'Inserting notifications...'

-- Task assignment notifications
INSERT INTO Notifications (UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, PayloadJson)
SELECT
    t.AssignedToUserId,
    N'مهمة جديدة',
    N'تم تكليفك بمهمة جديدة: ' + t.Title,
    0, -- TaskAssigned
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
    N'مرحباً بك في نظام FollowUp',
    N'تم تفعيل حسابك بنجاح. يمكنك الآن استلام المهام وتسجيل الحضور.',
    3, -- General
    1,
    1,
    DATEADD(DAY, -7, GETUTCDATE()),
    DATEADD(DAY, -7, GETUTCDATE()),
    '{}'
FROM Users
WHERE Role = 2;

PRINT 'Notifications inserted.'

-- =====================================================
-- FINAL SUMMARY
-- =====================================================
PRINT ''
PRINT '======================================'
PRINT 'AL-BIREH MUNICIPALITY SEED DATA COMPLETE'
PRINT '======================================'
PRINT ''

SELECT 'Summary' AS [Category], 'Count' AS [Value]
UNION ALL
SELECT '--------------------', '-----'
UNION ALL
SELECT 'Departments', CAST(COUNT(*) AS VARCHAR) FROM Departments
UNION ALL
SELECT 'Teams', CAST(COUNT(*) AS VARCHAR) FROM Teams
UNION ALL
SELECT 'Total Users', CAST(COUNT(*) AS VARCHAR) FROM Users
UNION ALL
SELECT '  - Admins', CAST(COUNT(*) AS VARCHAR) FROM Users WHERE Role = 0
UNION ALL
SELECT '  - Supervisors', CAST(COUNT(*) AS VARCHAR) FROM Users WHERE Role = 1
UNION ALL
SELECT '  - Workers', CAST(COUNT(*) AS VARCHAR) FROM Users WHERE Role = 2
UNION ALL
SELECT 'Tasks', CAST(COUNT(*) AS VARCHAR) FROM Tasks
UNION ALL
SELECT '  - Pending', CAST(COUNT(*) AS VARCHAR) FROM Tasks WHERE Status = 0
UNION ALL
SELECT '  - InProgress', CAST(COUNT(*) AS VARCHAR) FROM Tasks WHERE Status = 1
UNION ALL
SELECT '  - Completed', CAST(COUNT(*) AS VARCHAR) FROM Tasks WHERE Status = 2
UNION ALL
SELECT '  - Approved', CAST(COUNT(*) AS VARCHAR) FROM Tasks WHERE Status = 4
UNION ALL
SELECT 'Issues', CAST(COUNT(*) AS VARCHAR) FROM Issues
UNION ALL
SELECT 'Attendances', CAST(COUNT(*) AS VARCHAR) FROM Attendances
UNION ALL
SELECT 'Notifications', CAST(COUNT(*) AS VARCHAR) FROM Notifications;

PRINT ''
PRINT '======================================'
PRINT 'TEST CREDENTIALS (Password: Admin@123)'
PRINT '======================================'
PRINT ''
PRINT 'ADMIN:'
PRINT '  admin / Admin@123'
PRINT ''
PRINT 'SUPERVISORS:'
PRINT '  sup.sanitation1 / Admin@123 (Sanitation North)'
PRINT '  sup.sanitation2 / Admin@123 (Sanitation South)'
PRINT '  sup.publicworks1 / Admin@123 (Public Works)'
PRINT '  sup.publicworks2 / Admin@123 (Public Works)'
PRINT '  sup.agriculture / Admin@123 (Agriculture)'
PRINT '  sup.maintenance / Admin@123 (Maintenance)'
PRINT ''
PRINT 'SAMPLE WORKERS:'
PRINT '  san.worker1 - san.worker15 (Sanitation)'
PRINT '  pw.worker1 - pw.worker15 (Public Works)'
PRINT '  ag.worker1 - ag.worker10 (Agriculture)'
PRINT '  mnt.worker1 - mnt.worker5 (Maintenance)'
PRINT ''
PRINT '======================================'
GO
