-- ========================================================================
-- FOLLOWUP SYSTEM - AL-BIREH MUNICIPALITY REALISTIC SEED DATA
-- Based on actual municipality structure
-- ========================================================================
-- Structure:
--   Admin (مشرف) -> Supervisors (مراقبين) -> Workers (عمال)
--
-- Departments:
--   1. Health/Sanitation (الصحة): 100 workers, individual tasks by zone
--   2. Public Works (الأشغال): 30 workers in 6 teams of 5
--   3. Agriculture (الزراعة): 18 workers in 5 teams of 3-4
-- ========================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

USE FollowUpNew;

-- Clean existing data (order matters: children before parents)
DELETE FROM [Photos];
DELETE FROM [Notifications];
DELETE FROM [Appeals];
DELETE FROM [AuditLogs];
DELETE FROM [LocationHistories];
DELETE FROM [TaskTemplates];
DELETE FROM [GisFiles];
DELETE FROM [TwoFactorCodes];
DELETE FROM [RefreshTokens];
DELETE FROM [Issues];
DELETE FROM [Tasks];
DELETE FROM [Attendances];
DELETE FROM [UserZones];
DELETE FROM [Users];
DELETE FROM [Teams];
DELETE FROM [Zones];
DELETE FROM [Departments];
DELETE FROM [Municipalities];

DBCC CHECKIDENT ('[Municipalities]', RESEED, 0);
DBCC CHECKIDENT ('[Departments]', RESEED, 0);
DBCC CHECKIDENT ('[Teams]', RESEED, 0);
DBCC CHECKIDENT ('[Users]', RESEED, 0);
DBCC CHECKIDENT ('[Zones]', RESEED, 0);
DBCC CHECKIDENT ('[Tasks]', RESEED, 0);
DBCC CHECKIDENT ('[Issues]', RESEED, 0);

PRINT 'Data cleaned.';

-- ========================================================================
-- 1. MUNICIPALITY
-- ========================================================================
INSERT INTO [Municipalities] (
    Code, Name, NameEnglish, Country, Region,
    ContactEmail, ContactPhone, Address,
    MinLatitude, MaxLatitude, MinLongitude, MaxLongitude,
    DefaultStartTime, DefaultEndTime, DefaultGraceMinutes,
    MaxAcceptableAccuracyMeters, IsActive, CreatedAt
)
VALUES (
    'ALBIREH', N'بلدية البيرة', 'Al-Bireh Municipality',
    'Palestine', N'محافظة رام الله والبيرة',
    'info@albireh.ps', '+970-2-2406610', N'البيرة، فلسطين',
    31.88, 31.96, 35.18, 35.25,
    '07:00:00', '15:00:00', 15, 150.0, 1, GETUTCDATE()
);
DECLARE @MunicipalityId INT = SCOPE_IDENTITY();

-- ========================================================================
-- 2. DEPARTMENTS
-- ========================================================================
INSERT INTO [Departments] (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'دائرة الصحة والنظافة', 'Health & Sanitation', 'HEALTH', N'مسؤولة عن نظافة الشوارع وجمع النفايات - 100 عامل', 1, GETUTCDATE());
DECLARE @HealthDeptId INT = SCOPE_IDENTITY();

INSERT INTO [Departments] (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'دائرة الأشغال العامة', 'Public Works', 'WORKS', N'مسؤولة عن الصيانة والبنية التحتية - 30 عامل في 6 فرق', 1, GETUTCDATE());
DECLARE @WorksDeptId INT = SCOPE_IDENTITY();

INSERT INTO [Departments] (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'دائرة الزراعة', 'Agriculture', 'AGRI', N'مسؤولة عن الحدائق والمساحات الخضراء - 18 عامل في 5 فرق', 1, GETUTCDATE());
DECLARE @AgriDeptId INT = SCOPE_IDENTITY();

-- ========================================================================
-- 3. ZONES (Real Al-Bireh Neighborhoods)
-- ========================================================================
INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البصبوص', 'ZONE01', 'Al-Basbous', 31.896, 35.208, 125993, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z1 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'راس الطاحونة', 'ZONE02', 'Ras-Attahouneh', 31.907, 35.212, 104253, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z2 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'حديقة حرب', 'ZONE03', 'Hadiqat-Harb', 31.910, 35.212, 120466, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z3 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'قطعة شيبان', 'ZONE04', 'Qitat-Shayban', 31.912, 35.217, 110971, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z4 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البلدية', 'ZONE05', 'Al-Baladiyya', 31.907, 35.215, 54385, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z5 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البص', 'ZONE06', 'Al-Bass', 31.899, 35.213, 106870, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z6 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'راس حسين', 'ZONE07', 'Ras-Hsein', 31.897, 35.207, 131550, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z7 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الشيح الجنوبي', 'ZONE08', 'AsSheikh-AlJanubi', 31.889, 35.215, 147098, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z8 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الغربية', 'ZONE09', 'Al-Gharbieh', 31.910, 35.211, 121813, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z9 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'بئر الراس', 'ZONE10', 'Bir-ArRas', 31.916, 35.217, 148286, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z10 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الميدان', 'ZONE11', 'Al-Midan', 31.900, 35.209, 134508, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z11 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'سهل عواد', 'ZONE12', 'Sahl-Awwad', 31.911, 35.218, 85603, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z12 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'المركز', 'ZONE13', 'Al-Markaz', 31.911, 35.207, 103523, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z13 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الشيح الشمالي', 'ZONE14', 'Al-Sheikh-AsShamali', 31.892, 35.214, 168505, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z14 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الجور التحتا', 'ZONE15', 'Al-Jjuwar-AtTahta', 31.914, 35.219, 87603, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z15 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البلدة القديمة', 'ZONE16', 'Old-City', 31.905, 35.216, 84034, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z16 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'المنارة', 'ZONE17', 'Al-Manara', 31.903, 35.206, 91684, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z17 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الحومة', 'ZONE18', 'Al-Homa', 31.900, 35.215, 65365, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z18 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'المبارخ', 'ZONE19', 'Al-Mbarekh', 31.892, 35.208, 102778, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z19 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'القبارصة', 'ZONE20', 'Al-Qabarsah', 31.916, 35.220, 113248, N'البيرة', 1, GETUTCDATE(), 1, GETUTCDATE());
DECLARE @Z20 INT = SCOPE_IDENTITY();

PRINT 'Created 20 real Al-Bireh zones';

-- ========================================================================
-- 4. TEAMS (Works: 6 teams of 5, Agriculture: 5 teams of 3-4)
-- ========================================================================
-- Works Teams (6 teams)
INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق صيانة الطرق', 'ROAD1', 'Road Maintenance Team 1', 5, 1, GETUTCDATE());
DECLARE @WorksTeam1 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق صيانة الشبكات', 'INFRA1', 'Infrastructure Team', 5, 1, GETUTCDATE());
DECLARE @WorksTeam2 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق الإنارة', 'LIGHT1', 'Lighting Team', 5, 1, GETUTCDATE());
DECLARE @WorksTeam3 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق الطوارئ', 'EMERG1', 'Emergency Response Team', 5, 1, GETUTCDATE());
DECLARE @WorksTeam4 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق البناء', 'BUILD1', 'Construction Team', 5, 1, GETUTCDATE());
DECLARE @WorksTeam5 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق الصيانة العامة', 'MAINT1', 'General Maintenance', 5, 1, GETUTCDATE());
DECLARE @WorksTeam6 INT = SCOPE_IDENTITY();

-- Agriculture Teams (5 teams)
INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق الحدائق العامة', 'PARK1', 'Public Parks Team', 4, 1, GETUTCDATE());
DECLARE @AgriTeam1 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق التشجير', 'TREE1', 'Tree Planting Team', 4, 1, GETUTCDATE());
DECLARE @AgriTeam2 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق الري', 'IRRIG1', 'Irrigation Team', 4, 1, GETUTCDATE());
DECLARE @AgriTeam3 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق الصيانة الزراعية', 'AGMNT1', 'Agricultural Maintenance', 3, 1, GETUTCDATE());
DECLARE @AgriTeam4 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق المشاتل', 'NURSE1', 'Nursery Team', 3, 1, GETUTCDATE());
DECLARE @AgriTeam5 INT = SCOPE_IDENTITY();

PRINT 'Created 11 teams (6 Works + 5 Agriculture)';

-- ========================================================================
-- 5. USERS - Password Hashes
-- ========================================================================
-- All users use password: 'pass'
DECLARE @PassHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEAH6IPlGKjYFLen7QauEW7KaGXE01jw06DgomVLcvzffOVFTqDvhE4NZgiAi+ke7BQ==';

-- ========================================================================
-- 5.1 ADMIN
-- ========================================================================
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'admin', @PassHash, 'admin@albireh.ps', '+970599000001', N'مدير النظام', 0, NULL, NULL, 0, GETUTCDATE());
DECLARE @AdminId INT = SCOPE_IDENTITY();

-- ========================================================================
-- 5.2 SUPERVISORS (مراقبين)
-- Health: 5 supervisors (each manages ~20 workers)
-- Works: 2 supervisors (each manages ~15 workers)
-- Agriculture: 1 supervisor (manages 18 workers)
-- ========================================================================
-- Health Supervisors
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super1', @PassHash, 'health1@albireh.ps', '+970599010001', N'أحمد محمد - مراقب صحة 1', 1, NULL, @HealthDeptId, 0, GETUTCDATE());
DECLARE @HealthSup1 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super2', @PassHash, 'health2@albireh.ps', '+970599010002', N'خالد علي - مراقب صحة 2', 1, NULL, @HealthDeptId, 0, GETUTCDATE());
DECLARE @HealthSup2 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super3', @PassHash, 'health3@albireh.ps', '+970599010003', N'سامي حسن - مراقب صحة 3', 1, NULL, @HealthDeptId, 0, GETUTCDATE());
DECLARE @HealthSup3 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super4', @PassHash, 'health4@albireh.ps', '+970599010004', N'محمود عمر - مراقب صحة 4', 1, NULL, @HealthDeptId, 1, GETUTCDATE()); /* Inactive - transferred */
DECLARE @HealthSup4 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super5', @PassHash, 'health5@albireh.ps', '+970599010005', N'يوسف سعيد - مراقب صحة 5', 1, NULL, @HealthDeptId, 0, GETUTCDATE());
DECLARE @HealthSup5 INT = SCOPE_IDENTITY();

-- Works Supervisors
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super6', @PassHash, 'works1@albireh.ps', '+970599020001', N'طارق نبيل - مراقب أشغال 1', 1, NULL, @WorksDeptId, 0, GETUTCDATE());
DECLARE @WorksSup1 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super7', @PassHash, 'works2@albireh.ps', '+970599020002', N'فادي جمال - مراقب أشغال 2', 1, NULL, @WorksDeptId, 0, GETUTCDATE());
DECLARE @WorksSup2 INT = SCOPE_IDENTITY();

-- Agriculture Supervisor
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, Status, CreatedAt)
VALUES (@MunicipalityId, 'super8', @PassHash, 'agri1@albireh.ps', '+970599030001', N'وليد حسين - مراقب زراعة', 1, NULL, @AgriDeptId, 0, GETUTCDATE());
DECLARE @AgriSup1 INT = SCOPE_IDENTITY();

PRINT 'Created 8 supervisors';

-- ========================================================================
-- 5.3 HEALTH WORKERS (100 workers, distributed across 20 zones, ~5 per zone)
-- Individual workers, no teams, routine daily tasks
-- ========================================================================
DECLARE @i INT = 1;
DECLARE @ZoneId INT;
DECLARE @SupervisorId INT;
DECLARE @WorkerName NVARCHAR(100);

-- Create a temp table for zone IDs for easier random assignment
DECLARE @ZoneIds TABLE (idx INT IDENTITY(1,1), ZoneId INT);
INSERT INTO @ZoneIds (ZoneId) VALUES (@Z1),(@Z2),(@Z3),(@Z4),(@Z5),(@Z6),(@Z7),(@Z8),(@Z9),(@Z10),
                                     (@Z11),(@Z12),(@Z13),(@Z14),(@Z15),(@Z16),(@Z17),(@Z18),(@Z19),(@Z20);

-- Health Workers (100 total, ~20 per supervisor)
WHILE @i <= 100
BEGIN
    -- Assign supervisor based on worker number (20 workers per supervisor)
    SET @SupervisorId = CASE
        WHEN @i <= 20 THEN @HealthSup1
        WHEN @i <= 40 THEN @HealthSup2
        WHEN @i <= 60 THEN @HealthSup3
        WHEN @i <= 80 THEN @HealthSup4
        ELSE @HealthSup5
    END;

    -- Assign zone (5 workers per zone)
    SELECT @ZoneId = ZoneId FROM @ZoneIds WHERE idx = ((@i - 1) / 5) + 1;

    SET @WorkerName = N'عامل نظافة ' + CAST(@i AS NVARCHAR(10));

    INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, Status, CreatedAt)
    VALUES (@MunicipalityId, 'worker' + CAST(@i AS VARCHAR(10)), @PassHash, NULL,
            '+97059910' + RIGHT('0000' + CAST(@i AS VARCHAR(4)), 4), @WorkerName,
            2, 0, @HealthDeptId, @SupervisorId, 0, GETUTCDATE());

    SET @i = @i + 1;
END;

PRINT 'Created 100 health workers';

-- ========================================================================
-- 5.4 WORKS WORKERS (30 workers in 6 teams of 5)
-- ========================================================================
-- Team 1 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker101', @PassHash, '+970599200001', N'رامي طارق', 2, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker102', @PassHash, '+970599200002', N'ماجد سليم', 2, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker103', @PassHash, '+970599200003', N'هشام كمال', 2, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker104', @PassHash, '+970599200004', N'نادر جمال', 2, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker105', @PassHash, '+970599200005', N'زياد محمود', 2, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE());

-- Team 2 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker106', @PassHash, '+970599200006', N'باسم عادل', 2, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker107', @PassHash, '+970599200007', N'عماد خالد', 2, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker108', @PassHash, '+970599200008', N'سامر فيصل', 2, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker109', @PassHash, '+970599200009', N'أيمن راشد', 2, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker110', @PassHash, '+970599200010', N'حسام وليد', 2, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE());

-- Team 3 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker111', @PassHash, '+970599200011', N'فراس أحمد', 2, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker112', @PassHash, '+970599200012', N'معتز علي', 2, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker113', @PassHash, '+970599200013', N'ثائر محمد', 2, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker114', @PassHash, '+970599200014', N'وسام سعيد', 2, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker115', @PassHash, '+970599200015', N'حازم نبيل', 2, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE());

-- Team 4 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker116', @PassHash, '+970599200016', N'ياسر حسن', 2, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker117', @PassHash, '+970599200017', N'شادي عمر', 2, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker118', @PassHash, '+970599200018', N'أسامة طلال', 2, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker119', @PassHash, '+970599200019', N'مهند فادي', 2, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker120', @PassHash, '+970599200020', N'قصي رامي', 2, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 1, GETUTCDATE()) /* Inactive - resigned */;

-- Team 5 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker121', @PassHash, '+970599200021', N'براء خالد', 2, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker122', @PassHash, '+970599200022', N'أنس محمود', 2, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker123', @PassHash, '+970599200023', N'حمزة سامي', 2, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker124', @PassHash, '+970599200024', N'عبدالله يوسف', 2, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker125', @PassHash, '+970599200025', N'إياد ماهر', 2, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE());

-- Team 6 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker126', @PassHash, '+970599200026', N'كرم طارق', 2, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker127', @PassHash, '+970599200027', N'بشار عادل', 2, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker128', @PassHash, '+970599200028', N'غسان فيصل', 2, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker129', @PassHash, '+970599200029', N'صهيب راشد', 2, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker130', @PassHash, '+970599200030', N'مصعب وليد', 2, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE());

PRINT 'Created 30 works workers in 6 teams';

-- ========================================================================
-- 5.5 AGRICULTURE WORKERS (18 workers in 5 teams of 3-4)
-- ========================================================================
-- Team 1 (4 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker131', @PassHash, '+970599300001', N'منير أحمد', 2, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker132', @PassHash, '+970599300002', N'رائد محمد', 2, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker133', @PassHash, '+970599300003', N'نضال علي', 2, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker134', @PassHash, '+970599300004', N'جهاد خالد', 2, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE());

-- Team 2 (4 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker135', @PassHash, '+970599300005', N'عصام سامي', 2, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker136', @PassHash, '+970599300006', N'هيثم نبيل', 2, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker137', @PassHash, '+970599300007', N'لؤي طارق', 2, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker138', @PassHash, '+970599300008', N'رشيد عمر', 2, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE());

-- Team 3 (4 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker139', @PassHash, '+970599300009', N'حاتم فادي', 2, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker140', @PassHash, '+970599300010', N'مأمون رامي', 2, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker141', @PassHash, '+970599300011', N'صابر حسن', 2, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker142', @PassHash, '+970599300012', N'ناصر جمال', 2, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE());

-- Team 4 (3 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker143', @PassHash, '+970599300013', N'سهيل محمود', 2, 2, @AgriDeptId, @AgriTeam4, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker144', @PassHash, '+970599300014', N'عادل سعيد', 2, 2, @AgriDeptId, @AgriTeam4, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker145', @PassHash, '+970599300015', N'فيصل أحمد', 2, 2, @AgriDeptId, @AgriTeam4, @AgriSup1, 2, GETUTCDATE()) /* Suspended */;

-- Team 5 (3 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker146', @PassHash, '+970599300016', N'طلال كمال', 2, 2, @AgriDeptId, @AgriTeam5, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker147', @PassHash, '+970599300017', N'وائل نادر', 2, 2, @AgriDeptId, @AgriTeam5, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker148', @PassHash, '+970599300018', N'خليل ماجد', 2, 2, @AgriDeptId, @AgriTeam5, @AgriSup1, 0, GETUTCDATE());

PRINT 'Created 18 agriculture workers in 5 teams';

-- ========================================================================
-- 5.6 USER-ZONE ASSIGNMENTS (Health workers -> their zones)
-- ========================================================================
DECLARE @uzIdx INT = 1;
DECLARE @uzUserId INT;
DECLARE @uzZoneId INT;
DECLARE @uzSupervisorId INT;

WHILE @uzIdx <= 100
BEGIN
    SELECT @uzUserId = UserId FROM Users WHERE Username = 'worker' + CAST(@uzIdx AS VARCHAR(10));
    SELECT @uzZoneId = ZoneId FROM @ZoneIds WHERE idx = ((@uzIdx - 1) / 5) + 1;

    SET @uzSupervisorId = CASE
        WHEN @uzIdx <= 20 THEN @HealthSup1
        WHEN @uzIdx <= 40 THEN @HealthSup2
        WHEN @uzIdx <= 60 THEN @HealthSup3
        WHEN @uzIdx <= 80 THEN @HealthSup4
        ELSE @HealthSup5
    END;

    INSERT INTO [UserZones] (UserId, ZoneId, AssignedAt, AssignedByUserId, IsActive)
    VALUES (@uzUserId, @uzZoneId, GETUTCDATE(), @uzSupervisorId, 1);

    SET @uzIdx = @uzIdx + 1;
END;

PRINT 'Created 100 health worker zone assignments';

-- ========================================================================
-- 6. HISTORICAL TASKS (past 30 days of realistic data)
-- ========================================================================
DECLARE @HealthWorker1 INT, @HealthWorker2 INT, @HealthWorker3 INT, @HealthWorker4 INT, @HealthWorker5 INT;
SELECT TOP 1 @HealthWorker1 = UserId FROM Users WHERE Username = 'worker1';
SELECT TOP 1 @HealthWorker2 = UserId FROM Users WHERE Username = 'worker2';
SELECT TOP 1 @HealthWorker3 = UserId FROM Users WHERE Username = 'worker3';
SELECT TOP 1 @HealthWorker4 = UserId FROM Users WHERE Username = 'worker21';
SELECT TOP 1 @HealthWorker5 = UserId FROM Users WHERE Username = 'worker41';

DECLARE @WorksWorker1 INT, @WorksWorker2 INT, @WorksWorker11 INT;
SELECT TOP 1 @WorksWorker1 = UserId FROM Users WHERE Username = 'worker101';
SELECT TOP 1 @WorksWorker2 = UserId FROM Users WHERE Username = 'worker106';
SELECT TOP 1 @WorksWorker11 = UserId FROM Users WHERE Username = 'worker111';

DECLARE @AgriWorker1 INT, @AgriWorker2 INT;
SELECT TOP 1 @AgriWorker1 = UserId FROM Users WHERE Username = 'worker131';
SELECT TOP 1 @AgriWorker2 = UserId FROM Users WHERE Username = 'worker135';

-- ── 6.1 Completed & Approved tasks (3-4 weeks ago) ──
-- GPS coords have realistic ±10-200m offset from zone centers (not exact copies)
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف شارع الميدان الرئيسي', N'تنظيف يومي شامل', @Z11, @HealthWorker2, @HealthSup1, 1, 3, 1, 31.9013, 35.2084, 50, DATEADD(DAY,-27,GETUTCDATE()), DATEADD(DAY,-28,GETUTCDATE()), DATEADD(DAY,-27,GETUTCDATE()), DATEADD(DAY,-27,GETUTCDATE()), N'تم التنظيف بالكامل', 100, 1, 2, DATEADD(DAY,-28,GETUTCDATE())),
(@MunicipalityId, N'جمع نفايات - البصبوص', N'جولة جمع صباحية', @Z1, @HealthWorker1, @HealthSup1, 1, 3, 0, 31.8952, 35.2093, 50, DATEADD(DAY,-26,GETUTCDATE()), DATEADD(DAY,-27,GETUTCDATE()), DATEADD(DAY,-26,GETUTCDATE()), DATEADD(DAY,-26,GETUTCDATE()), N'تم جمع جميع الحاويات', 100, 1, 2, DATEADD(DAY,-27,GETUTCDATE())),
(@MunicipalityId, N'تعقيم حاويات - راس الطاحونة', N'تعقيم وغسل 12 حاوية', @Z2, @HealthWorker3, @HealthSup1, 1, 3, 2, 31.9063, 35.2131, 50, DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-26,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE()), N'تم تعقيم 12 حاوية', 100, 1, 2, DATEADD(DAY,-26,GETUTCDATE())),
(@MunicipalityId, N'تنظيف ساحة البلدية', N'تنظيف شامل للساحة والمحيط', @Z5, @HealthWorker1, @HealthSup1, 2, 3, 4, 31.9076, 35.2143, 50, DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), N'تم بنجاح', 100, 1, 2, DATEADD(DAY,-25,GETUTCDATE())),
(@MunicipalityId, N'جمع نفايات - حديقة حرب', N'جمع روتيني', @Z3, @HealthWorker4, @HealthSup2, 1, 3, 0, 31.9108, 35.2108, 50, DATEADD(DAY,-23,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-23,GETUTCDATE()), DATEADD(DAY,-23,GETUTCDATE()), N'تم', 100, 1, 2, DATEADD(DAY,-24,GETUTCDATE())),
(@MunicipalityId, N'كنس شارع القبارصة', N'كنس وتنظيف الرصيف', @Z20, @HealthWorker5, @HealthSup3, 1, 3, 1, 31.9154, 35.2212, 50, DATEADD(DAY,-22,GETUTCDATE()), DATEADD(DAY,-23,GETUTCDATE()), DATEADD(DAY,-22,GETUTCDATE()), DATEADD(DAY,-22,GETUTCDATE()), N'تنظيف كامل', 100, 1, 2, DATEADD(DAY,-23,GETUTCDATE())),
(@MunicipalityId, N'تنظيف حاويات - البص', N'غسل وتعقيم', @Z6, @HealthWorker2, @HealthSup1, 1, 3, 2, 31.8997, 35.2122, 50, DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-21,GETUTCDATE()), DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-20,GETUTCDATE()), N'تم تنظيف 8 حاويات', 100, 1, 2, DATEADD(DAY,-21,GETUTCDATE())),
(@MunicipalityId, N'جمع نفايات - بئر الراس', N'جولة يومية', @Z10, @HealthWorker3, @HealthSup1, 1, 3, 0, 31.9168, 35.2163, 50, DATEADD(DAY,-19,GETUTCDATE()), DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-19,GETUTCDATE()), DATEADD(DAY,-19,GETUTCDATE()), N'تم بنجاح', 100, 1, 2, DATEADD(DAY,-20,GETUTCDATE()));

-- Completed & Approved works team tasks
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, TeamId, IsTeamTask, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'إصلاح أنبوب مياه - الغربية', N'تسرب مياه في الشارع الرئيسي', @Z9, @WorksWorker1, @WorksTeam1, 1, @WorksSup1, 3, 3, 3, 31.9094, 35.2118, 100, DATEADD(DAY,-26,GETUTCDATE()), DATEADD(DAY,-27,GETUTCDATE()), DATEADD(DAY,-26,GETUTCDATE()), DATEADD(DAY,-26,GETUTCDATE()), N'تم إصلاح التسرب واستبدال القطعة', 100, 1, 2, DATEADD(DAY,-27,GETUTCDATE())),
(@MunicipalityId, N'صيانة إنارة شارع المركز', N'استبدال 3 لمبات معطلة', @Z13, @WorksWorker11, @WorksTeam3, 1, @WorksSup1, 1, 3, 3, 31.9118, 35.2063, 150, DATEADD(DAY,-22,GETUTCDATE()), DATEADD(DAY,-23,GETUTCDATE()), DATEADD(DAY,-22,GETUTCDATE()), DATEADD(DAY,-22,GETUTCDATE()), N'تم استبدال 3 لمبات LED', 100, 1, 2, DATEADD(DAY,-23,GETUTCDATE())),
(@MunicipalityId, N'إصلاح رصيف - البلدة القديمة', N'بلاط متكسر يشكل خطر', @Z16, @WorksWorker1, @WorksTeam1, 1, @WorksSup1, 2, 3, 3, 31.9043, 35.2167, 100, DATEADD(DAY,-18,GETUTCDATE()), DATEADD(DAY,-19,GETUTCDATE()), DATEADD(DAY,-18,GETUTCDATE()), DATEADD(DAY,-18,GETUTCDATE()), N'تم إعادة تبليط 15 متر', 100, 1, 2, DATEADD(DAY,-19,GETUTCDATE()));

-- Completed & Approved agri team tasks
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, TeamId, IsTeamTask, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تقليم أشجار شارع المنارة', N'تقليم 20 شجرة على جانبي الشارع', @Z17, @AgriWorker1, @AgriTeam1, 1, @AgriSup1, 1, 3, 4, 31.9024, 35.2068, 200, DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), N'تم تقليم 20 شجرة', 100, 1, 2, DATEADD(DAY,-25,GETUTCDATE())),
(@MunicipalityId, N'زراعة ورود - ساحة البلدية', N'زراعة 50 شتلة ورد موسمي', @Z5, @AgriWorker2, @AgriTeam2, 1, @AgriSup1, 1, 3, 4, 31.9065, 35.2157, 200, DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-21,GETUTCDATE()), DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-20,GETUTCDATE()), N'تم زراعة 50 شتلة', 100, 1, 2, DATEADD(DAY,-21,GETUTCDATE()));

-- ── 6.2 Completed tasks (past 2 weeks), some approved, some awaiting review ──
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف شارع الحومة', N'تنظيف يومي', @Z18, @HealthWorker1, @HealthSup1, 1, 3, 1, 31.9006, 35.2142, 50, DATEADD(DAY,-14,GETUTCDATE()), DATEADD(DAY,-15,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE()), N'تم', 100, 1, 3, DATEADD(DAY,-15,GETUTCDATE())),
(@MunicipalityId, N'جمع نفايات - المبارخ', N'جولة صباحية', @Z19, @HealthWorker4, @HealthSup2, 1, 3, 0, 31.8914, 35.2087, 50, DATEADD(DAY,-13,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE()), DATEADD(DAY,-13,GETUTCDATE()), DATEADD(DAY,-13,GETUTCDATE()), N'تم جمع كافة النفايات', 100, 1, 3, DATEADD(DAY,-14,GETUTCDATE())),
(@MunicipalityId, N'تنظيف حاويات - سهل عواد', N'تعقيم 10 حاويات', @Z12, @HealthWorker2, @HealthSup1, 1, 3, 2, 31.9104, 35.2187, 50, DATEADD(DAY,-10,GETUTCDATE()), DATEADD(DAY,-11,GETUTCDATE()), DATEADD(DAY,-10,GETUTCDATE()), DATEADD(DAY,-10,GETUTCDATE()), N'تم تعقيم 10 حاويات', 100, 1, 3, DATEADD(DAY,-11,GETUTCDATE())),
-- Completed but awaiting supervisor approval
(@MunicipalityId, N'كنس شارع الشيح الجنوبي', N'كنس الشارع الرئيسي', @Z8, @HealthWorker5, @HealthSup3, 1, 2, 1, 31.8884, 35.2158, 50, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-4,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE()), N'تم الكنس بالكامل', 100, 1, 2, DATEADD(DAY,-4,GETUTCDATE())),
(@MunicipalityId, N'جمع نفايات - راس حسين', N'جولة مسائية', @Z7, @HealthWorker3, @HealthSup1, 1, 2, 0, 31.8975, 35.2063, 50, DATEADD(DAY,-2,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE()), N'تم الجمع من 15 حاوية', 100, 1, 2, DATEADD(DAY,-3,GETUTCDATE()));

-- ── 6.3 Rejected tasks (supervisor rejected completion) ──
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف حاويات - قطعة شيبان', N'غسل وتعقيم 8 حاويات', @Z4, @HealthWorker1, @HealthSup1, 1, 4, 2, 31.9127, 35.2162, 50, DATEADD(DAY,-15,GETUTCDATE()), DATEADD(DAY,-16,GETUTCDATE()), DATEADD(DAY,-15,GETUTCDATE()), DATEADD(DAY,-15,GETUTCDATE()), N'تم التنظيف', 100, 0, N'الصور تظهر حاويات لم تنظف بالكامل', DATEADD(DAY,-14,GETUTCDATE()), @HealthSup1, 1, 3, DATEADD(DAY,-16,GETUTCDATE())),
(@MunicipalityId, N'جمع نفايات - الجور التحتا', N'جولة صباحية', @Z15, @HealthWorker4, @HealthSup2, 1, 4, 0, 31.9147, 35.2183, 50, DATEADD(DAY,-8,GETUTCDATE()), DATEADD(DAY,-9,GETUTCDATE()), DATEADD(DAY,-8,GETUTCDATE()), DATEADD(DAY,-8,GETUTCDATE()), N'تم', 100, 1, N'المسافة بعيدة عن موقع المهمة - رفض تلقائي', DATEADD(DAY,-8,GETUTCDATE()), NULL, 1, 3, DATEADD(DAY,-9,GETUTCDATE()));

-- ── 6.4 In-Progress tasks (recent) ──
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, ProgressPercentage, ProgressNotes, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف شارع البلدية الرئيسي', N'تنظيف يومي روتيني', @Z5, @HealthWorker1, @HealthSup1, 1, 1, 1, 31.9082, 35.2146, 50, DATEADD(DAY, 0, GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()), DATEADD(HOUR,-2,GETUTCDATE()), 40, N'بدأت من الجهة الشمالية', 1, 1, DATEADD(DAY,-1,GETUTCDATE())),
(@MunicipalityId, N'جمع النفايات - حي الميدان', N'جولة جمع صباحية', @Z11, @HealthWorker2, @HealthSup1, 1, 1, 0, 31.8993, 35.2097, 50, DATEADD(DAY, 0, GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()), DATEADD(HOUR,-1,GETUTCDATE()), 60, N'وصلت للمنتصف', 1, 1, DATEADD(DAY,-1,GETUTCDATE()));

INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, TeamId, IsTeamTask, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, ProgressPercentage, ProgressNotes, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'إصلاح حفرة شارع المنارة', N'ردم وإصلاح حفرة كبيرة', @Z17, @WorksWorker1, @WorksTeam1, 1, @WorksSup1, 2, 1, 3, 31.9035, 35.2053, 100, DATEADD(DAY, 1, GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()), DATEADD(HOUR,-3,GETUTCDATE()), 30, N'تم الحفر وبدأنا بالردم', 1, 1, DATEADD(DAY,-1,GETUTCDATE())),
(@MunicipalityId, N'ري حديقة حرب', N'ري الأشجار والمسطحات الخضراء', @Z3, @AgriWorker1, @AgriTeam1, 1, @AgriSup1, 1, 1, 4, 31.9093, 35.2127, 200, DATEADD(DAY, 0, GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()), DATEADD(HOUR,-1,GETUTCDATE()), 50, N'تم ري القسم الشرقي', 1, 1, DATEADD(DAY,-1,GETUTCDATE()));

-- ── 6.5 Pending tasks (assigned today/tomorrow) ──
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف حاويات - البلدة القديمة', N'تعقيم وتنظيف الحاويات', @Z16, @HealthWorker3, @HealthSup1, 1, 0, 2, 31.9056, 35.2153, 50, DATEADD(DAY, 1, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'كنس شارع الشيح الشمالي', N'كنس وتنظيف', @Z14, @HealthWorker5, @HealthSup3, 1, 0, 1, 31.8926, 35.2133, 50, DATEADD(DAY, 1, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'تفتيش حاويات - البص', N'تفتيش حالة الحاويات وتقرير', @Z6, @HealthWorker1, @HealthSup1, 0, 0, 5, 31.8984, 35.2138, 50, DATEADD(DAY, 2, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE());

INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, TeamId, IsTeamTask, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'صيانة إنارة حي الغربية', N'استبدال 5 أعمدة إنارة معطلة', @Z9, @WorksWorker11, @WorksTeam3, 1, @WorksSup1, 1, 0, 3, 31.9107, 35.2103, 150, DATEADD(DAY, 2, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'زراعة أشجار في المركز', N'زراعة 15 شجرة زيتون', @Z13, @AgriWorker1, @AgriTeam2, 1, @AgriSup1, 1, 0, 4, 31.9103, 35.2078, 200, DATEADD(DAY, 3, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE());

-- One cancelled task
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف ساحة المنارة', N'ألغيت بسبب أعمال بناء في المنطقة', @Z17, @HealthWorker4, @HealthSup2, 1, 4, 4, 31.9037, 35.2054, 50, DATEADD(DAY,-10,GETUTCDATE()), DATEADD(DAY,-12,GETUTCDATE()), 1, 1, DATEADD(DAY,-12,GETUTCDATE()));

PRINT 'Created 30 historical tasks (13 completed, 2 under review, 3 rejected, 4 in-progress, 5 pending, 2 works+agri extra)';

-- ========================================================================
-- 7. HISTORICAL ISSUES (past 30 days)
-- ========================================================================
INSERT INTO [Issues] (MunicipalityId, Title, Description, ReportedByUserId, ZoneId, Type, Severity, Status, Latitude, Longitude, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, IsSynced, SyncVersion, ReportedAt)
VALUES
-- Resolved issues (3-4 weeks ago) - GPS scattered within zone
(@MunicipalityId, N'حفرة كبيرة في شارع البصبوص', N'حفرة عميقة تشكل خطر على المشاة', @HealthWorker1, @Z1, 1, 3, 2, 31.8967, 35.2074, DATEADD(DAY,-28,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE()), N'تم ردم الحفرة وإصلاح الشارع', @WorksSup1, 1, 2, DATEADD(DAY,-28,GETUTCDATE())),
(@MunicipalityId, N'تراكم نفايات خلف المدرسة', N'نفايات متراكمة منذ أسبوع', @HealthWorker2, @Z5, 3, 2, 2, 31.9078, 35.2138, DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), N'تم تنظيف المنطقة بالكامل', @HealthSup1, 1, 2, DATEADD(DAY,-25,GETUTCDATE())),
(@MunicipalityId, N'إنارة معطلة في الحومة', N'3 أعمدة إنارة لا تعمل في الشارع الرئيسي', @WorksWorker1, @Z18, 1, 2, 2, 31.9008, 35.2143, DATEADD(DAY,-22,GETUTCDATE()), DATEADD(DAY,-20,GETUTCDATE()), N'تم استبدال اللمبات', @WorksSup1, 1, 2, DATEADD(DAY,-22,GETUTCDATE())),
(@MunicipalityId, N'شجرة مائلة على الرصيف', N'شجرة كبيرة مائلة قد تسقط', @AgriWorker1, @Z3, 2, 3, 2, 31.9113, 35.2114, DATEADD(DAY,-20,GETUTCDATE()), DATEADD(DAY,-18,GETUTCDATE()), N'تم تقليم الأغصان وتثبيت الشجرة', @AgriSup1, 1, 2, DATEADD(DAY,-20,GETUTCDATE())),
(@MunicipalityId, N'حاوية محترقة', N'حاوية نفايات محترقة بالكامل في المبارخ', @HealthWorker5, @Z19, 4, 2, 2, 31.8928, 35.2073, DATEADD(DAY,-17,GETUTCDATE()), DATEADD(DAY,-15,GETUTCDATE()), N'تم استبدال الحاوية بأخرى جديدة', @HealthSup3, 1, 2, DATEADD(DAY,-17,GETUTCDATE()));

-- Dismissed issues
INSERT INTO [Issues] (MunicipalityId, Title, Description, ReportedByUserId, ZoneId, Type, Severity, Status, Latitude, Longitude, EventTime, ResolutionNotes, IsSynced, SyncVersion, ReportedAt)
VALUES
(@MunicipalityId, N'ضوضاء من ورشة بناء', N'ضوضاء عالية من ورشة مجاورة', @HealthWorker3, @Z12, 5, 1, 2, 31.9116, 35.2173, DATEADD(DAY,-18,GETUTCDATE()), N'خارج اختصاص البلدية - تم تحويلها للجهة المختصة', 1, 2, DATEADD(DAY,-18,GETUTCDATE())),
(@MunicipalityId, N'بلاغ مكرر - حفرة البصبوص', N'نفس البلاغ السابق', @HealthWorker1, @Z1, 1, 2, 2, 31.8955, 35.2088, DATEADD(DAY,-16,GETUTCDATE()), N'بلاغ مكرر - تم معالجته سابقاً', 1, 2, DATEADD(DAY,-16,GETUTCDATE()));

-- Under review issues (recent)
INSERT INTO [Issues] (MunicipalityId, Title, Description, ReportedByUserId, ZoneId, Type, Severity, Status, Latitude, Longitude, EventTime, IsSynced, SyncVersion, ReportedAt)
VALUES
(@MunicipalityId, N'تسرب مياه صرف صحي', N'تسرب في شارع الغربية الفرعي', @WorksWorker2, @Z9, 1, 3, 1, 31.9096, 35.2117, DATEADD(DAY,-5,GETUTCDATE()), 1, 1, DATEADD(DAY,-5,GETUTCDATE())),
(@MunicipalityId, N'رصيف مكسور بجانب المسجد', N'بلاط مكسور يشكل خطر على كبار السن', @WorksWorker1, @Z16, 1, 2, 1, 31.9044, 35.2168, DATEADD(DAY,-3,GETUTCDATE()), 1, 1, DATEADD(DAY,-3,GETUTCDATE())),
(@MunicipalityId, N'أغصان متدلية على الشارع', N'أغصان شجرة تعيق حركة المركبات', @AgriWorker2, @Z10, 2, 2, 1, 31.9153, 35.2178, DATEADD(DAY,-2,GETUTCDATE()), 1, 1, DATEADD(DAY,-2,GETUTCDATE()));

-- Forwarded issue
INSERT INTO [Issues] (MunicipalityId, Title, Description, ReportedByUserId, ZoneId, Type, Severity, Status, Latitude, Longitude, EventTime, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion, ReportedAt)
VALUES
(@MunicipalityId, N'حفرة وتسرب مياه معاً', N'حفرة ناتجة عن تسرب مياه - تحتاج تنسيق بين الأقسام', @HealthWorker3, @Z7, 1, 3, 1, 31.8963, 35.2077, DATEADD(DAY,-4,GETUTCDATE()), @WorksDeptId, DATEADD(DAY,-3,GETUTCDATE()), N'تم تحويلها لقسم الأشغال لأنها تتعلق بالبنية التحتية', @HealthSup1, 1, 1, DATEADD(DAY,-4,GETUTCDATE()));

-- Recently reported (today/yesterday)
INSERT INTO [Issues] (MunicipalityId, Title, Description, ReportedByUserId, ZoneId, Type, Severity, Status, Latitude, Longitude, EventTime, IsSynced, SyncVersion, ReportedAt)
VALUES
(@MunicipalityId, N'تراكم نفايات بجانب المدرسة', N'شكوى من سكان المنطقة - نفايات متراكمة', @HealthWorker1, @Z5, 3, 2, 0, 31.9064, 35.2156, DATEADD(HOUR,-5,GETUTCDATE()), 1, 1, DATEADD(HOUR,-5,GETUTCDATE())),
(@MunicipalityId, N'عمود إنارة مائل', N'عمود إنارة مائل بشكل خطير بعد حادث سير', @WorksWorker1, @Z17, 2, 4, 0, 31.9025, 35.2048, DATEADD(HOUR,-2,GETUTCDATE()), 1, 1, DATEADD(HOUR,-2,GETUTCDATE()));

PRINT 'Created 16 historical issues (7 resolved, 4 forwarded, 2 new)';

-- ========================================================================
-- 8. ATTENDANCE RECORDS (past 20 work days: Sun-Thu)
-- ========================================================================
DECLARE @dayOffset INT = -27; -- start from ~4 weeks ago
DECLARE @dayOfWeek INT;
DECLARE @checkIn DATETIME;
DECLARE @checkOut DATETIME;
DECLARE @workDays INT = 0;

WHILE @workDays < 20 AND @dayOffset <= -1
BEGIN
    SET @dayOfWeek = DATEPART(WEEKDAY, DATEADD(DAY, @dayOffset, GETUTCDATE()));
    -- Skip Friday(6) and Saturday(7) (Palestinian weekend)
    IF @dayOfWeek NOT IN (6, 7)
    BEGIN
        SET @checkIn = DATEADD(MINUTE, 420 + (ABS(CHECKSUM(NEWID())) % 20), DATEADD(DAY, @dayOffset, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME))); -- ~7:00-7:20 AM
        SET @checkOut = DATEADD(HOUR, 8, @checkIn); -- ~8 hours later

        -- Worker1 (Health/Z1) - reliable, check-in near zone start, check-out at different spot
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, WorkDuration, Status, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType, ApprovalStatus)
        VALUES (@MunicipalityId, @HealthWorker1, @Z1, @checkIn, @checkIn, @checkOut, @checkOut,
                31.8953 + (ABS(CHECKSUM(NEWID())) % 15) * 0.0001, 35.2074 + (ABS(CHECKSUM(NEWID())) % 12) * 0.0001, 12.0 + (ABS(CHECKSUM(NEWID())) % 80) * 0.1,
                31.8968 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.2087 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 10.0 + (ABS(CHECKSUM(NEWID())) % 90) * 0.1,
                1, DATEADD(HOUR, 8, 0) - 0, 2, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved');

        -- Worker2 (Health/Z2) - sometimes late
        SET @checkIn = DATEADD(MINUTE, 420 + (ABS(CHECKSUM(NEWID())) % 35), DATEADD(DAY, @dayOffset, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME))); -- 7:00-7:35
        SET @checkOut = DATEADD(HOUR, 8, @checkIn);
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, WorkDuration, Status, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType, ApprovalStatus)
        VALUES (@MunicipalityId, @HealthWorker2, @Z2,
                @checkIn, @checkIn, @checkOut, @checkOut,
                31.9064 + (ABS(CHECKSUM(NEWID())) % 12) * 0.0001, 35.2113 + (ABS(CHECKSUM(NEWID())) % 14) * 0.0001, 14.0 + (ABS(CHECKSUM(NEWID())) % 85) * 0.1,
                31.9078 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.2126 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 11.0 + (ABS(CHECKSUM(NEWID())) % 75) * 0.1,
                1, DATEADD(HOUR, 8, 0) - 0, 2, 1, 1,
                CASE WHEN DATEPART(MINUTE, @checkIn) > 15 THEN DATEPART(MINUTE, @checkIn) - 0 ELSE 0 END,
                0, 0,
                CASE WHEN DATEPART(MINUTE, @checkIn) > 15 THEN 'Late' ELSE 'OnTime' END,
                'AutoApproved');

        -- Worker101 (Works) - moves between job sites
        SET @checkIn = DATEADD(MINUTE, 420 + (ABS(CHECKSUM(NEWID())) % 15), DATEADD(DAY, @dayOffset, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)));
        SET @checkOut = DATEADD(HOUR, 8, @checkIn);
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, WorkDuration, Status, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType, ApprovalStatus)
        VALUES (@MunicipalityId, @WorksWorker1, NULL,
                @checkIn, @checkIn, @checkOut, @checkOut,
                31.9023 + (ABS(CHECKSUM(NEWID())) % 15) * 0.0001, 35.2053 + (ABS(CHECKSUM(NEWID())) % 14) * 0.0001, 9.0 + (ABS(CHECKSUM(NEWID())) % 60) * 0.1,
                31.9047 + (ABS(CHECKSUM(NEWID())) % 12) * 0.0001, 35.2068 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 8.0 + (ABS(CHECKSUM(NEWID())) % 70) * 0.1,
                1, DATEADD(HOUR, 8, 0) - 0, 2, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved');

        -- Worker131 (Agri) - moves within park/garden areas
        SET @checkIn = DATEADD(MINUTE, 420 + (ABS(CHECKSUM(NEWID())) % 20), DATEADD(DAY, @dayOffset, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)));
        SET @checkOut = DATEADD(HOUR, 8, @checkIn);
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, WorkDuration, Status, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType, ApprovalStatus)
        VALUES (@MunicipalityId, @AgriWorker1, NULL,
                @checkIn, @checkIn, @checkOut, @checkOut,
                31.9094 + (ABS(CHECKSUM(NEWID())) % 12) * 0.0001, 35.2113 + (ABS(CHECKSUM(NEWID())) % 14) * 0.0001, 11.0 + (ABS(CHECKSUM(NEWID())) % 65) * 0.1,
                31.9108 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.2128 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 10.0 + (ABS(CHECKSUM(NEWID())) % 55) * 0.1,
                1, DATEADD(HOUR, 8, 0) - 0, 2, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved');

        SET @workDays = @workDays + 1;
    END;

    SET @dayOffset = @dayOffset + 1;
END;

-- Today's active check-ins (still checked in, no check-out yet)
SET @checkIn = DATEADD(MINUTE, 425, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME)); -- 7:05 AM today
INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, IsValidated, Status, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType, ApprovalStatus)
VALUES
(@MunicipalityId, @HealthWorker1, @Z1, @checkIn, @checkIn, 31.8957, 35.2083, 13.2, 1, 1, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved'),
(@MunicipalityId, @HealthWorker2, @Z2, DATEADD(MINUTE, 5, @checkIn), DATEADD(MINUTE, 5, @checkIn), 31.9072, 35.2118, 17.8, 1, 1, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved'),
(@MunicipalityId, @WorksWorker1, NULL, @checkIn, @checkIn, 31.9038, 35.2054, 10.4, 1, 1, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved'),
(@MunicipalityId, @AgriWorker1, NULL, DATEADD(MINUTE, 3, @checkIn), DATEADD(MINUTE, 3, @checkIn), 31.9097, 35.2124, 12.7, 1, 1, 1, 1, 0, 0, 0, 'OnTime', 'AutoApproved');

PRINT 'Created ~84 attendance records (20 days x 4 workers + 4 active today)';

-- ========================================================================
-- 9. NOTIFICATIONS
-- ========================================================================
INSERT INTO [Notifications] (MunicipalityId, UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, ReadAt)
VALUES
-- Old read notifications (Type: 1=TaskAssigned, 3=TaskUpdated, 4=IssueReviewed)
(@MunicipalityId, @HealthWorker1, N'مهمة جديدة', N'تم تعيين مهمة تنظيف شارع البلدية الرئيسي', 1, 1, 1, DATEADD(DAY,-28,GETUTCDATE()), DATEADD(DAY,-28,GETUTCDATE()), DATEADD(DAY,-28,GETUTCDATE())),
(@MunicipalityId, @HealthWorker1, N'مهمة معتمدة', N'تم اعتماد مهمتك: تنظيف ساحة البلدية', 3, 1, 1, DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE()), DATEADD(DAY,-24,GETUTCDATE())),
(@MunicipalityId, @HealthWorker1, N'مهمة مرفوضة', N'تم رفض مهمتك: تنظيف حاويات قطعة شيبان - الصور غير كافية', 3, 1, 1, DATEADD(DAY,-14,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE())),
(@MunicipalityId, @WorksWorker1, N'مهمة جديدة', N'تم تعيين مهمة إصلاح أنبوب مياه - الغربية', 1, 1, 1, DATEADD(DAY,-27,GETUTCDATE()), DATEADD(DAY,-27,GETUTCDATE()), DATEADD(DAY,-26,GETUTCDATE())),
(@MunicipalityId, @WorksWorker1, N'مهمة معتمدة', N'تم اعتماد مهمتك: إصلاح رصيف البلدة القديمة', 3, 1, 1, DATEADD(DAY,-17,GETUTCDATE()), DATEADD(DAY,-17,GETUTCDATE()), DATEADD(DAY,-17,GETUTCDATE())),
(@MunicipalityId, @AgriWorker1, N'مهمة جديدة', N'تم تعيين مهمة تقليم أشجار شارع المنارة', 1, 1, 1, DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE())),
-- Supervisor notifications
(@MunicipalityId, @HealthSup1, N'بلاغ جديد', N'بلاغ جديد: تراكم نفايات بجانب المدرسة في منطقة البلدية', 4, 1, 1, DATEADD(HOUR,-5,GETUTCDATE()), DATEADD(HOUR,-5,GETUTCDATE()), DATEADD(HOUR,-4,GETUTCDATE())),
(@MunicipalityId, @WorksSup1, N'بلاغ محول', N'تم تحويل بلاغ حفرة وتسرب مياه إلى قسمكم', 4, 1, 1, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-3,GETUTCDATE())),
-- Recent unread notifications
(@MunicipalityId, @HealthWorker1, N'مهمة جديدة', N'تم تعيين مهمة تفتيش حاويات - البص', 1, 0, 1, GETUTCDATE(), GETUTCDATE(), NULL),
(@MunicipalityId, @WorksWorker11, N'مهمة جديدة', N'تم تعيين مهمة صيانة إنارة حي الغربية', 1, 0, 1, GETUTCDATE(), GETUTCDATE(), NULL),
(@MunicipalityId, @HealthSup1, N'مهمة بانتظار المراجعة', N'العامل أكمل مهمة جمع نفايات راس حسين - بحاجة مراجعة', 3, 0, 1, DATEADD(DAY,-2,GETUTCDATE()), DATEADD(DAY,-2,GETUTCDATE()), NULL),
(@MunicipalityId, @WorksSup1, N'بلاغ جديد', N'بلاغ عاجل: عمود إنارة مائل في المنارة', 4, 0, 1, DATEADD(HOUR,-2,GETUTCDATE()), DATEADD(HOUR,-2,GETUTCDATE()), NULL);

PRINT 'Created 12 notifications (7 read + 5 unread)';

-- ========================================================================
-- 10. APPEALS
-- ========================================================================
-- Get the rejected task IDs
DECLARE @RejectedTask1 INT, @RejectedTask2 INT;
SELECT TOP 1 @RejectedTask1 = TaskId FROM Tasks WHERE Status = 4 AND AssignedToUserId = @HealthWorker1;
SELECT TOP 1 @RejectedTask2 = TaskId FROM Tasks WHERE Status = 4 AND AssignedToUserId = @HealthWorker4;

-- Appeal 1: Approved (worker was right, supervisor re-checked photos)
INSERT INTO [Appeals] (MunicipalityId, AppealType, EntityType, EntityId, UserId, WorkerExplanation, WorkerLatitude, WorkerLongitude, ExpectedLatitude, ExpectedLongitude, DistanceMeters, Status, ReviewedByUserId, ReviewedAt, ReviewNotes, SubmittedAt, CreatedAt, IsSynced, SyncVersion)
VALUES (@MunicipalityId, 1, 'Task', @RejectedTask1, @HealthWorker1,
    N'قمت بتنظيف جميع الحاويات - ربما الصور لم تكن واضحة بسبب الإضاءة. أرفق صور إضافية',
    31.912, 35.217, 31.912, 35.217, 5,
    2, @HealthSup1, DATEADD(DAY,-13,GETUTCDATE()), N'تم مراجعة الصور الإضافية - فعلاً تم التنظيف. اعتذار للعامل.',
    DATEADD(DAY,-14,GETUTCDATE()), DATEADD(DAY,-14,GETUTCDATE()), 1, 1);

-- Appeal 2: Rejected (auto-rejection was correct, worker was far from location)
INSERT INTO [Appeals] (MunicipalityId, AppealType, EntityType, EntityId, UserId, WorkerExplanation, WorkerLatitude, WorkerLongitude, ExpectedLatitude, ExpectedLongitude, DistanceMeters, Status, ReviewedByUserId, ReviewedAt, ReviewNotes, SubmittedAt, CreatedAt, IsSynced, SyncVersion)
VALUES (@MunicipalityId, 1, 'Task', @RejectedTask2, @HealthWorker4,
    N'كنت في المنطقة لكن GPS الهاتف أعطى موقع غلط',
    31.920, 35.225, 31.914, 35.219, 750,
    3, @HealthSup2, DATEADD(DAY,-7,GETUTCDATE()), N'المسافة 750 متر - كبيرة جداً حتى مع خطأ GPS. يرجى الالتزام بالموقع.',
    DATEADD(DAY,-8,GETUTCDATE()), DATEADD(DAY,-8,GETUTCDATE()), 1, 1);

-- Appeal 3: Pending (recent, awaiting supervisor review)
INSERT INTO [Appeals] (MunicipalityId, AppealType, EntityType, EntityId, UserId, WorkerExplanation, WorkerLatitude, WorkerLongitude, ExpectedLatitude, ExpectedLongitude, DistanceMeters, Status, SubmittedAt, CreatedAt, IsSynced, SyncVersion)
VALUES (@MunicipalityId, 1, 'Task', @RejectedTask2, @HealthWorker4,
    N'أطلب إعادة النظر - كان هناك تحويلة في الشارع أجبرتني على تغيير المسار',
    31.916, 35.221, 31.914, 35.219, 250,
    1, DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-1,GETUTCDATE()), 1, 1);

PRINT 'Created 3 appeals (1 approved, 1 rejected, 1 pending)';

-- ========================================================================
-- 11. AUDIT LOGS (login attempts, task actions, issue reports over past 30 days)
-- ========================================================================
INSERT INTO [AuditLogs] (UserId, Username, Action, Details, IpAddress, UserAgent, CreatedAt)
VALUES
-- Admin logins
(@AdminId, N'admin', N'Login', N'تسجيل دخول ناجح', '192.168.1.10', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-28,GETUTCDATE())),
(@AdminId, N'admin', N'Login', N'تسجيل دخول ناجح', '192.168.1.10', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-21,GETUTCDATE())),
(@AdminId, N'admin', N'Login', N'تسجيل دخول ناجح', '192.168.1.10', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-14,GETUTCDATE())),
(@AdminId, N'admin', N'Login', N'تسجيل دخول ناجح', '192.168.1.10', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-7,GETUTCDATE())),
(@AdminId, N'admin', N'Login', N'تسجيل دخول ناجح', '192.168.1.10', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-1,GETUTCDATE())),
-- Supervisor logins
(@HealthSup1, N'super1', N'Login', N'تسجيل دخول ناجح', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-27,GETUTCDATE())),
(@HealthSup1, N'super1', N'Login', N'تسجيل دخول ناجح', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-20,GETUTCDATE())),
(@HealthSup1, N'super1', N'Login', N'تسجيل دخول ناجح', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-13,GETUTCDATE())),
(@HealthSup1, N'super1', N'Login', N'تسجيل دخول ناجح', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-6,GETUTCDATE())),
(@HealthSup2, N'super2', N'Login', N'تسجيل دخول ناجح', '10.0.0.22', 'FollowUp/1.0 Android', DATEADD(DAY,-25,GETUTCDATE())),
(@HealthSup2, N'super2', N'Login', N'تسجيل دخول ناجح', '10.0.0.22', 'FollowUp/1.0 Android', DATEADD(DAY,-18,GETUTCDATE())),
(@WorksSup1, N'super6', N'Login', N'تسجيل دخول ناجح', '10.0.0.30', 'FollowUp/1.0 Android', DATEADD(DAY,-26,GETUTCDATE())),
(@WorksSup1, N'super6', N'Login', N'تسجيل دخول ناجح', '10.0.0.30', 'FollowUp/1.0 Android', DATEADD(DAY,-12,GETUTCDATE())),
(@AgriSup1, N'super8', N'Login', N'تسجيل دخول ناجح', '10.0.0.40', 'FollowUp/1.0 Android', DATEADD(DAY,-24,GETUTCDATE())),
-- Worker logins (sample)
(@HealthWorker1, N'worker1', N'Login', N'تسجيل دخول ناجح', '10.0.1.1', 'FollowUp/1.0 Android', DATEADD(DAY,-27,GETUTCDATE())),
(@HealthWorker1, N'worker1', N'Login', N'تسجيل دخول ناجح', '10.0.1.1', 'FollowUp/1.0 Android', DATEADD(DAY,-20,GETUTCDATE())),
(@HealthWorker1, N'worker1', N'Login', N'تسجيل دخول ناجح', '10.0.1.1', 'FollowUp/1.0 Android', DATEADD(DAY,-13,GETUTCDATE())),
(@HealthWorker2, N'worker2', N'Login', N'تسجيل دخول ناجح', '10.0.1.2', 'FollowUp/1.0 Android', DATEADD(DAY,-26,GETUTCDATE())),
(@HealthWorker2, N'worker2', N'Login', N'تسجيل دخول ناجح', '10.0.1.2', 'FollowUp/1.0 Android', DATEADD(DAY,-19,GETUTCDATE())),
(@HealthWorker3, N'worker3', N'Login', N'تسجيل دخول ناجح', '10.0.1.3', 'FollowUp/1.0 Android', DATEADD(DAY,-25,GETUTCDATE())),
(@WorksWorker1, N'worker101', N'Login', N'تسجيل دخول ناجح', '10.0.2.1', 'FollowUp/1.0 Android', DATEADD(DAY,-26,GETUTCDATE())),
(@AgriWorker1, N'worker131', N'Login', N'تسجيل دخول ناجح', '10.0.3.1', 'FollowUp/1.0 Android', DATEADD(DAY,-24,GETUTCDATE())),
-- Failed login attempt
(NULL, N'unknown_user', N'LoginFailed', N'محاولة تسجيل دخول فاشلة - اسم مستخدم غير موجود', '85.112.40.55', 'Mozilla/5.0', DATEADD(DAY,-15,GETUTCDATE())),
(NULL, N'admin', N'LoginFailed', N'محاولة تسجيل دخول فاشلة - كلمة مرور خاطئة', '85.112.40.55', 'Mozilla/5.0', DATEADD(DAY,-15,GETUTCDATE())),
-- Task actions
(@HealthSup1, N'super1', N'CreateTask', N'إنشاء مهمة: تنظيف شارع الميدان الرئيسي', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-28,GETUTCDATE())),
(@HealthSup1, N'super1', N'CreateTask', N'إنشاء مهمة: جمع نفايات - البصبوص', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-27,GETUTCDATE())),
(@HealthSup1, N'super1', N'ApproveTask', N'اعتماد مهمة: تنظيف شارع الميدان الرئيسي', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-27,GETUTCDATE())),
(@HealthSup1, N'super1', N'RejectTask', N'رفض مهمة: تنظيف حاويات - قطعة شيبان', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-14,GETUTCDATE())),
(@WorksSup1, N'super6', N'CreateTask', N'إنشاء مهمة: إصلاح أنبوب مياه - الغربية', '10.0.0.30', 'FollowUp/1.0 Android', DATEADD(DAY,-27,GETUTCDATE())),
(@WorksSup1, N'super6', N'ApproveTask', N'اعتماد مهمة: إصلاح أنبوب مياه - الغربية', '10.0.0.30', 'FollowUp/1.0 Android', DATEADD(DAY,-26,GETUTCDATE())),
-- Issue actions
(@HealthWorker1, N'worker1', N'ReportIssue', N'بلاغ: حفرة كبيرة في شارع البصبوص', '10.0.1.1', 'FollowUp/1.0 Android', DATEADD(DAY,-28,GETUTCDATE())),
(@WorksWorker1, N'worker101', N'ReportIssue', N'بلاغ: إنارة معطلة في الحومة', '10.0.2.1', 'FollowUp/1.0 Android', DATEADD(DAY,-22,GETUTCDATE())),
(@HealthSup1, N'super1', N'ForwardIssue', N'تحويل بلاغ: حفرة وتسرب مياه معاً - لقسم الأشغال', '10.0.0.15', 'FollowUp/1.0 Android', DATEADD(DAY,-3,GETUTCDATE())),
-- Attendance actions
(@HealthWorker1, N'worker1', N'CheckIn', N'تسجيل حضور تلقائي - منطقة البصبوص', '10.0.1.1', 'FollowUp/1.0 Android', DATEADD(DAY,-2,GETUTCDATE())),
(@HealthWorker1, N'worker1', N'CheckOut', N'تسجيل انصراف تلقائي - غادر المنطقة', '10.0.1.1', 'FollowUp/1.0 Android', DATEADD(DAY,-2,GETUTCDATE())),
-- Password reset
(NULL, N'worker5', N'ForgotPassword', N'طلب إعادة تعيين كلمة المرور', '10.0.1.5', 'FollowUp/1.0 Android', DATEADD(DAY,-10,GETUTCDATE())),
(NULL, N'worker5', N'ResetPassword', N'تم إعادة تعيين كلمة المرور بنجاح', '10.0.1.5', 'FollowUp/1.0 Android', DATEADD(DAY,-10,GETUTCDATE()));

PRINT 'Created 36 audit log entries (logins, tasks, issues, attendance, password resets)';

-- ========================================================================
-- 12. LOCATION HISTORIES (GPS tracking samples - past 3 days for active workers)
-- ========================================================================
-- Worker 1 (Health) - Day -2, morning route through zone البصبوص
INSERT INTO [LocationHistories] (UserId, Latitude, Longitude, Speed, Accuracy, Heading, Timestamp, IsSync)
VALUES
(@HealthWorker1, 31.8952, 35.2093, 0.5, 8.0, 0, DATEADD(HOUR,-50,GETUTCDATE()), 1),
(@HealthWorker1, 31.8955, 35.2088, 1.2, 6.5, 45, DATEADD(MINUTE,-2995,GETUTCDATE()), 1),
(@HealthWorker1, 31.8960, 35.2082, 1.8, 7.0, 30, DATEADD(MINUTE,-2990,GETUTCDATE()), 1),
(@HealthWorker1, 31.8965, 35.2078, 2.0, 5.5, 20, DATEADD(MINUTE,-2985,GETUTCDATE()), 1),
(@HealthWorker1, 31.8968, 35.2075, 0.3, 6.0, 0, DATEADD(MINUTE,-2980,GETUTCDATE()), 1),
(@HealthWorker1, 31.8968, 35.2074, 0.1, 8.0, 0, DATEADD(MINUTE,-2975,GETUTCDATE()), 1),
(@HealthWorker1, 31.8970, 35.2070, 1.5, 5.0, 310, DATEADD(MINUTE,-2970,GETUTCDATE()), 1),
(@HealthWorker1, 31.8958, 35.2090, 1.8, 6.0, 150, DATEADD(MINUTE,-2965,GETUTCDATE()), 1),
-- Worker 2 (Health) - Day -2, route through zone الميدان
(@HealthWorker2, 31.9013, 35.2084, 0.2, 7.0, 0, DATEADD(HOUR,-49,GETUTCDATE()), 1),
(@HealthWorker2, 31.9018, 35.2080, 1.5, 5.5, 60, DATEADD(MINUTE,-2935,GETUTCDATE()), 1),
(@HealthWorker2, 31.9025, 35.2075, 2.1, 6.0, 45, DATEADD(MINUTE,-2930,GETUTCDATE()), 1),
(@HealthWorker2, 31.9030, 35.2078, 1.0, 7.5, 90, DATEADD(MINUTE,-2925,GETUTCDATE()), 1),
(@HealthWorker2, 31.9030, 35.2079, 0.1, 8.0, 0, DATEADD(MINUTE,-2920,GETUTCDATE()), 1),
(@HealthWorker2, 31.9025, 35.2085, 1.8, 5.5, 200, DATEADD(MINUTE,-2915,GETUTCDATE()), 1),
-- Worker 101 (Works) - Day -1, at repair site in الغربية
(@WorksWorker1, 31.9094, 35.2118, 0.0, 4.0, 0, DATEADD(HOUR,-30,GETUTCDATE()), 1),
(@WorksWorker1, 31.9094, 35.2119, 0.1, 5.0, 0, DATEADD(MINUTE,-1795,GETUTCDATE()), 1),
(@WorksWorker1, 31.9095, 35.2117, 0.2, 4.5, 0, DATEADD(MINUTE,-1790,GETUTCDATE()), 1),
(@WorksWorker1, 31.9093, 35.2120, 0.1, 6.0, 0, DATEADD(MINUTE,-1785,GETUTCDATE()), 1),
(@WorksWorker1, 31.9096, 35.2115, 1.5, 5.0, 270, DATEADD(MINUTE,-1780,GETUTCDATE()), 1),
(@WorksWorker1, 31.9100, 35.2110, 2.0, 5.5, 315, DATEADD(MINUTE,-1775,GETUTCDATE()), 1),
-- Worker 131 (Agri) - Day -1, moving through المنارة
(@AgriWorker1, 31.9024, 35.2068, 0.5, 9.0, 0, DATEADD(HOUR,-28,GETUTCDATE()), 1),
(@AgriWorker1, 31.9028, 35.2063, 1.0, 7.0, 330, DATEADD(MINUTE,-1675,GETUTCDATE()), 1),
(@AgriWorker1, 31.9035, 35.2058, 1.5, 6.0, 340, DATEADD(MINUTE,-1670,GETUTCDATE()), 1),
(@AgriWorker1, 31.9040, 35.2055, 0.8, 8.0, 350, DATEADD(MINUTE,-1665,GETUTCDATE()), 1),
-- Today - active workers
(@HealthWorker1, 31.8950, 35.2095, 0.3, 6.0, 0, DATEADD(HOUR,-2,GETUTCDATE()), 1),
(@HealthWorker1, 31.8953, 35.2090, 1.0, 5.0, 40, DATEADD(MINUTE,-115,GETUTCDATE()), 1),
(@HealthWorker1, 31.8958, 35.2085, 1.5, 5.5, 35, DATEADD(MINUTE,-110,GETUTCDATE()), 1),
(@HealthWorker2, 31.9010, 35.2088, 0.2, 7.0, 0, DATEADD(HOUR,-1,GETUTCDATE()), 1),
(@HealthWorker2, 31.9015, 35.2082, 1.2, 6.0, 50, DATEADD(MINUTE,-55,GETUTCDATE()), 1);

PRINT 'Created 29 location history records (3 days of GPS tracking for 4 workers)';

-- ========================================================================
-- 13. PHOTOS (task completion + issue report evidence)
-- ========================================================================
-- Get actual base IDs (handles DBCC RESEED quirk on fresh vs populated tables)
DECLARE @TaskBase INT = (SELECT MIN(TaskId) FROM Tasks);
DECLARE @IssueBase INT = (SELECT MIN(IssueId) FROM Issues);

-- Task completion photos (completed tasks have evidence photos)
INSERT INTO [Photos] (PhotoUrl, EntityType, EntityId, TaskId, OrderIndex, FileSizeBytes, UploadedAt, UploadedByUserId, CreatedAt)
VALUES
-- Photos for completed task "تنظيف شارع الميدان الرئيسي" (1st task)
(N'/uploads/photos/tasks/task_clean_street_1a.jpg', N'Task', @TaskBase, @TaskBase, 0, 245000, DATEADD(DAY,-27,GETUTCDATE()), @HealthWorker2, DATEADD(DAY,-27,GETUTCDATE())),
(N'/uploads/photos/tasks/task_clean_street_1b.jpg', N'Task', @TaskBase, @TaskBase, 1, 312000, DATEADD(DAY,-27,GETUTCDATE()), @HealthWorker2, DATEADD(DAY,-27,GETUTCDATE())),
-- Photos for "جمع نفايات - البصبوص" (2nd task)
(N'/uploads/photos/tasks/task_collect_waste_2a.jpg', N'Task', @TaskBase+1, @TaskBase+1, 0, 198000, DATEADD(DAY,-26,GETUTCDATE()), @HealthWorker1, DATEADD(DAY,-26,GETUTCDATE())),
-- Photos for "تعقيم حاويات - راس الطاحونة" (3rd task)
(N'/uploads/photos/tasks/task_sanitize_bins_3a.jpg', N'Task', @TaskBase+2, @TaskBase+2, 0, 267000, DATEADD(DAY,-25,GETUTCDATE()), @HealthWorker3, DATEADD(DAY,-25,GETUTCDATE())),
(N'/uploads/photos/tasks/task_sanitize_bins_3b.jpg', N'Task', @TaskBase+2, @TaskBase+2, 1, 289000, DATEADD(DAY,-25,GETUTCDATE()), @HealthWorker3, DATEADD(DAY,-25,GETUTCDATE())),
-- Photos for works team task "إصلاح أنبوب مياه" (9th task)
(N'/uploads/photos/tasks/task_pipe_repair_before.jpg', N'Task', @TaskBase+8, @TaskBase+8, 0, 410000, DATEADD(DAY,-26,GETUTCDATE()), @WorksWorker1, DATEADD(DAY,-26,GETUTCDATE())),
(N'/uploads/photos/tasks/task_pipe_repair_after.jpg', N'Task', @TaskBase+8, @TaskBase+8, 1, 385000, DATEADD(DAY,-26,GETUTCDATE()), @WorksWorker1, DATEADD(DAY,-26,GETUTCDATE())),
-- Photos for agri task "تقليم أشجار شارع المنارة" (12th task)
(N'/uploads/photos/tasks/task_tree_trim_a.jpg', N'Task', @TaskBase+11, @TaskBase+11, 0, 520000, DATEADD(DAY,-24,GETUTCDATE()), @AgriWorker1, DATEADD(DAY,-24,GETUTCDATE())),
-- Photos for rejected task "تنظيف حاويات - قطعة شيبان" (19th task)
(N'/uploads/photos/tasks/task_bins_rejected_blurry.jpg', N'Task', @TaskBase+18, @TaskBase+18, 0, 156000, DATEADD(DAY,-15,GETUTCDATE()), @HealthWorker1, DATEADD(DAY,-15,GETUTCDATE()));

-- Issue report photos
INSERT INTO [Photos] (PhotoUrl, EntityType, EntityId, IssueId, OrderIndex, FileSizeBytes, UploadedAt, UploadedByUserId, CreatedAt)
VALUES
-- "حفرة كبيرة في شارع البصبوص" (1st issue)
(N'/uploads/photos/issues/issue_pothole_1a.jpg', N'Issue', @IssueBase, @IssueBase, 0, 340000, DATEADD(DAY,-28,GETUTCDATE()), @HealthWorker1, DATEADD(DAY,-28,GETUTCDATE())),
(N'/uploads/photos/issues/issue_pothole_1b.jpg', N'Issue', @IssueBase, @IssueBase, 1, 298000, DATEADD(DAY,-28,GETUTCDATE()), @HealthWorker1, DATEADD(DAY,-28,GETUTCDATE())),
-- "تراكم نفايات خلف المدرسة" (2nd issue)
(N'/uploads/photos/issues/issue_waste_school_2a.jpg', N'Issue', @IssueBase+1, @IssueBase+1, 0, 275000, DATEADD(DAY,-25,GETUTCDATE()), @HealthWorker2, DATEADD(DAY,-25,GETUTCDATE())),
-- "حاوية محترقة" (5th issue)
(N'/uploads/photos/issues/issue_burnt_bin_5a.jpg', N'Issue', @IssueBase+4, @IssueBase+4, 0, 380000, DATEADD(DAY,-17,GETUTCDATE()), @HealthWorker5, DATEADD(DAY,-17,GETUTCDATE())),
(N'/uploads/photos/issues/issue_burnt_bin_5b.jpg', N'Issue', @IssueBase+4, @IssueBase+4, 1, 425000, DATEADD(DAY,-17,GETUTCDATE()), @HealthWorker5, DATEADD(DAY,-17,GETUTCDATE())),
-- "تسرب مياه صرف صحي" (8th issue)
(N'/uploads/photos/issues/issue_sewage_leak.jpg', N'Issue', @IssueBase+7, @IssueBase+7, 0, 310000, DATEADD(DAY,-5,GETUTCDATE()), @WorksWorker2, DATEADD(DAY,-5,GETUTCDATE())),
-- "عمود إنارة مائل" (last issue)
(N'/uploads/photos/issues/issue_tilted_pole.jpg', N'Issue', @IssueBase+12, @IssueBase+12, 0, 445000, DATEADD(HOUR,-2,GETUTCDATE()), @WorksWorker1, DATEADD(HOUR,-2,GETUTCDATE()));

PRINT 'Created 16 photos (9 task evidence + 7 issue reports)';

-- ========================================================================
-- 14. TASK TEMPLATES (recurring daily/weekly operations)
-- ========================================================================
INSERT INTO [TaskTemplates] (Title, Description, MunicipalityId, ZoneId, Frequency, Time, IsActive, LastGeneratedAt, CreatedAt)
VALUES
-- Daily health tasks
(N'جمع نفايات يومي - البصبوص', N'جولة جمع نفايات صباحية يومية في منطقة البصبوص', @MunicipalityId, @Z1, N'Daily', '06:00:00', 1, DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-30,GETUTCDATE())),
(N'جمع نفايات يومي - الميدان', N'جولة جمع نفايات صباحية يومية في منطقة الميدان', @MunicipalityId, @Z11, N'Daily', '06:00:00', 1, DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-30,GETUTCDATE())),
(N'تنظيف شارع البلدية الرئيسي', N'كنس وتنظيف يومي للشارع الرئيسي', @MunicipalityId, @Z5, N'Daily', '06:30:00', 1, DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-28,GETUTCDATE())),
(N'جمع نفايات يومي - حديقة حرب', N'جولة جمع وتنظيف محيط الحديقة', @MunicipalityId, @Z3, N'Daily', '07:00:00', 1, DATEADD(DAY,-1,GETUTCDATE()), DATEADD(DAY,-25,GETUTCDATE())),
-- Weekly health tasks
(N'تعقيم حاويات - راس الطاحونة', N'غسل وتعقيم جميع الحاويات في المنطقة', @MunicipalityId, @Z2, N'Weekly', '07:00:00', 1, DATEADD(DAY,-5,GETUTCDATE()), DATEADD(DAY,-30,GETUTCDATE())),
(N'تعقيم حاويات - البص', N'غسل وتعقيم الحاويات', @MunicipalityId, @Z6, N'Weekly', '07:00:00', 1, DATEADD(DAY,-4,GETUTCDATE()), DATEADD(DAY,-28,GETUTCDATE())),
-- Weekly agriculture task
(N'ري المسطحات الخضراء - حديقة حرب', N'ري الأشجار والمسطحات الخضراء في الحديقة', @MunicipalityId, @Z3, N'Weekly', '06:00:00', 1, DATEADD(DAY,-3,GETUTCDATE()), DATEADD(DAY,-27,GETUTCDATE())),
-- Monthly tasks
(N'فحص شبكة الإنارة - المنطقة الوسطى', N'فحص شامل لجميع أعمدة الإنارة وتوثيق الأعطال', @MunicipalityId, @Z13, N'Monthly', '08:00:00', 1, DATEADD(DAY,-15,GETUTCDATE()), DATEADD(DAY,-30,GETUTCDATE())),
-- Inactive template (discontinued)
(N'تنظيف ساحة المنارة', N'ألغيت بسبب أعمال بناء في المنطقة', @MunicipalityId, @Z17, N'Daily', '06:00:00', 0, DATEADD(DAY,-12,GETUTCDATE()), DATEADD(DAY,-30,GETUTCDATE()));

PRINT 'Created 9 task templates (4 daily, 3 weekly, 1 monthly, 1 inactive)';

-- ========================================================================
-- SUMMARY
-- ========================================================================
PRINT '========================================';
PRINT 'AL-BIREH MUNICIPALITY SEED DATA COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'Municipality: Al-Bireh';
PRINT 'Zones: 20 (real neighborhoods)';
PRINT 'Departments: 3';
PRINT '';
PRINT 'USERS (154 active, 3 inactive):';
PRINT '  Admin: 1 (admin)';
PRINT '  Supervisors: 8 (super1-super8)';
PRINT '  Workers: 148 (worker1-worker148)';
PRINT '    - Health: 100 (individual, by zone)';
PRINT '    - Works: 30 (6 teams of 5)';
PRINT '    - Agriculture: 18 (5 teams of 3-4)';
PRINT '  Inactive: super4 (transferred), worker120 (resigned), worker145 (suspended)';
PRINT '';
PRINT 'Teams: 11 (6 Works + 5 Agriculture)';
PRINT 'Zone Assignments: 100 (Health workers mapped to zones)';
PRINT '';
PRINT 'HISTORICAL DATA (past 30 days):';
PRINT '  Tasks: 30 (13 completed, 2 under-review, 3 rejected, 4 in-progress, 5 pending)';
PRINT '  Issues: 13 (7 resolved, 4 forwarded, 2 new)';
PRINT '  Attendance: ~84 records (20 days x 4 workers + 4 active today)';
PRINT '  Notifications: 12 (7 read + 5 unread)';
PRINT '  Appeals: 3 (1 approved, 1 rejected, 1 pending)';
PRINT '  Audit Logs: 36 (logins, task actions, issue reports, password resets)';
PRINT '  Location History: 29 GPS tracking points (3 days, 4 workers)';
PRINT '  Photos: 16 (9 task evidence + 7 issue reports)';
PRINT '  Task Templates: 9 (4 daily, 3 weekly, 1 monthly, 1 inactive)';
PRINT '';
PRINT 'LOGIN CREDENTIALS (all passwords: pass):';
PRINT '  Admin: admin / pass';
PRINT '  Supervisors: super1..super8 / pass';
PRINT '    super1-5 = Health, super6-7 = Works, super8 = Agri';
PRINT '  Workers: worker1..worker148 / pass';
PRINT '    worker1-100 = Health, worker101-130 = Works, worker131-148 = Agri';
PRINT '========================================';
