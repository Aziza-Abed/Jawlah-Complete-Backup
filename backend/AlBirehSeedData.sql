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

-- Clean existing data
DELETE FROM [Notifications];
DELETE FROM [Appeals];
DELETE FROM [Issues];
DELETE FROM [Photos];
DELETE FROM [Tasks];
DELETE FROM [Attendances];
DELETE FROM [UserZones];
DELETE FROM [AuditLogs];
DELETE FROM [GisFiles];
DELETE FROM [LocationHistories];
DELETE FROM [TaskTemplates];
DELETE FROM [TwoFactorCodes];
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
VALUES (@MunicipalityId, 'admin', @PassHash, 'admin@albireh.ps', '+970599000001', N'مدير النظام', 2, NULL, NULL, 0, GETUTCDATE());
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
            0, 0, @HealthDeptId, @SupervisorId, 0, GETUTCDATE());

    SET @i = @i + 1;
END;

PRINT 'Created 100 health workers';

-- ========================================================================
-- 5.4 WORKS WORKERS (30 workers in 6 teams of 5)
-- ========================================================================
-- Team 1 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker101', @PassHash, '+970599200001', N'رامي طارق', 0, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker102', @PassHash, '+970599200002', N'ماجد سليم', 0, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker103', @PassHash, '+970599200003', N'هشام كمال', 0, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker104', @PassHash, '+970599200004', N'نادر جمال', 0, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker105', @PassHash, '+970599200005', N'زياد محمود', 0, 1, @WorksDeptId, @WorksTeam1, @WorksSup1, 0, GETUTCDATE());

-- Team 2 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker106', @PassHash, '+970599200006', N'باسم عادل', 0, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker107', @PassHash, '+970599200007', N'عماد خالد', 0, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker108', @PassHash, '+970599200008', N'سامر فيصل', 0, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker109', @PassHash, '+970599200009', N'أيمن راشد', 0, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker110', @PassHash, '+970599200010', N'حسام وليد', 0, 1, @WorksDeptId, @WorksTeam2, @WorksSup1, 0, GETUTCDATE());

-- Team 3 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker111', @PassHash, '+970599200011', N'فراس أحمد', 0, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker112', @PassHash, '+970599200012', N'معتز علي', 0, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker113', @PassHash, '+970599200013', N'ثائر محمد', 0, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker114', @PassHash, '+970599200014', N'وسام سعيد', 0, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker115', @PassHash, '+970599200015', N'حازم نبيل', 0, 1, @WorksDeptId, @WorksTeam3, @WorksSup1, 0, GETUTCDATE());

-- Team 4 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker116', @PassHash, '+970599200016', N'ياسر حسن', 0, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker117', @PassHash, '+970599200017', N'شادي عمر', 0, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker118', @PassHash, '+970599200018', N'أسامة طلال', 0, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker119', @PassHash, '+970599200019', N'مهند فادي', 0, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker120', @PassHash, '+970599200020', N'قصي رامي', 0, 1, @WorksDeptId, @WorksTeam4, @WorksSup2, 1, GETUTCDATE()) /* Inactive - resigned */;

-- Team 5 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker121', @PassHash, '+970599200021', N'براء خالد', 0, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker122', @PassHash, '+970599200022', N'أنس محمود', 0, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker123', @PassHash, '+970599200023', N'حمزة سامي', 0, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker124', @PassHash, '+970599200024', N'عبدالله يوسف', 0, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker125', @PassHash, '+970599200025', N'إياد ماهر', 0, 1, @WorksDeptId, @WorksTeam5, @WorksSup2, 0, GETUTCDATE());

-- Team 6 (5 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker126', @PassHash, '+970599200026', N'كرم طارق', 0, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker127', @PassHash, '+970599200027', N'بشار عادل', 0, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker128', @PassHash, '+970599200028', N'غسان فيصل', 0, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker129', @PassHash, '+970599200029', N'صهيب راشد', 0, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE()),
(@MunicipalityId, 'worker130', @PassHash, '+970599200030', N'مصعب وليد', 0, 1, @WorksDeptId, @WorksTeam6, @WorksSup2, 0, GETUTCDATE());

PRINT 'Created 30 works workers in 6 teams';

-- ========================================================================
-- 5.5 AGRICULTURE WORKERS (18 workers in 5 teams of 3-4)
-- ========================================================================
-- Team 1 (4 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker131', @PassHash, '+970599300001', N'منير أحمد', 0, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker132', @PassHash, '+970599300002', N'رائد محمد', 0, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker133', @PassHash, '+970599300003', N'نضال علي', 0, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker134', @PassHash, '+970599300004', N'جهاد خالد', 0, 2, @AgriDeptId, @AgriTeam1, @AgriSup1, 0, GETUTCDATE());

-- Team 2 (4 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker135', @PassHash, '+970599300005', N'عصام سامي', 0, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker136', @PassHash, '+970599300006', N'هيثم نبيل', 0, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker137', @PassHash, '+970599300007', N'لؤي طارق', 0, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker138', @PassHash, '+970599300008', N'رشيد عمر', 0, 2, @AgriDeptId, @AgriTeam2, @AgriSup1, 0, GETUTCDATE());

-- Team 3 (4 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker139', @PassHash, '+970599300009', N'حاتم فادي', 0, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker140', @PassHash, '+970599300010', N'مأمون رامي', 0, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker141', @PassHash, '+970599300011', N'صابر حسن', 0, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker142', @PassHash, '+970599300012', N'ناصر جمال', 0, 2, @AgriDeptId, @AgriTeam3, @AgriSup1, 0, GETUTCDATE());

-- Team 4 (3 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker143', @PassHash, '+970599300013', N'سهيل محمود', 0, 2, @AgriDeptId, @AgriTeam4, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker144', @PassHash, '+970599300014', N'عادل سعيد', 0, 2, @AgriDeptId, @AgriTeam4, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker145', @PassHash, '+970599300015', N'فيصل أحمد', 0, 2, @AgriDeptId, @AgriTeam4, @AgriSup1, 2, GETUTCDATE()) /* Suspended */;

-- Team 5 (3 workers)
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, PhoneNumber, FullName, Role, WorkerType, DepartmentId, TeamId, SupervisorId, Status, CreatedAt)
VALUES
(@MunicipalityId, 'worker146', @PassHash, '+970599300016', N'طلال كمال', 0, 2, @AgriDeptId, @AgriTeam5, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker147', @PassHash, '+970599300017', N'وائل نادر', 0, 2, @AgriDeptId, @AgriTeam5, @AgriSup1, 0, GETUTCDATE()),
(@MunicipalityId, 'worker148', @PassHash, '+970599300018', N'خليل ماجد', 0, 2, @AgriDeptId, @AgriTeam5, @AgriSup1, 0, GETUTCDATE());

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
-- 6. SAMPLE TASKS
-- ========================================================================
-- Get some worker IDs for task assignment
DECLARE @HealthWorker1 INT, @HealthWorker2 INT, @HealthWorker3 INT;
SELECT TOP 1 @HealthWorker1 = UserId FROM Users WHERE Username = 'worker1';
SELECT TOP 1 @HealthWorker2 = UserId FROM Users WHERE Username = 'worker2';
SELECT TOP 1 @HealthWorker3 = UserId FROM Users WHERE Username = 'worker3';

DECLARE @WorksWorker1 INT;
SELECT TOP 1 @WorksWorker1 = UserId FROM Users WHERE Username = 'worker101';

DECLARE @WorksWorker11 INT;
SELECT TOP 1 @WorksWorker11 = UserId FROM Users WHERE Username = 'worker111';

DECLARE @AgriWorker1 INT;
SELECT TOP 1 @AgriWorker1 = UserId FROM Users WHERE Username = 'worker131';

-- Health routine tasks (individual)
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تنظيف شارع البلدية الرئيسي', N'تنظيف يومي روتيني', @Z5, @HealthWorker1, @HealthSup1, 1, 0, 0, 31.907, 35.215, 50, DATEADD(DAY, 1, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'جمع النفايات - حي الميدان', N'جولة جمع صباحية', @Z11, @HealthWorker2, @HealthSup1, 1, 0, 0, 31.900, 35.209, 50, DATEADD(DAY, 1, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'تنظيف حاويات - البلدة القديمة', N'تعقيم وتنظيف الحاويات', @Z16, @HealthWorker3, @HealthSup1, 1, 1, 0, 31.905, 35.216, 50, DATEADD(DAY, 1, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE());

-- Works team tasks
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, TeamId, IsTeamTask, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'إصلاح حفرة شارع المنارة', N'ردم وإصلاح حفرة كبيرة تعيق حركة المرور', @Z17, @WorksWorker1, @WorksTeam1, 1, @WorksSup1, 2, 0, 1, 31.903, 35.206, 100, DATEADD(DAY, 2, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'صيانة إنارة حي الغربية', N'استبدال 5 أعمدة إنارة معطلة', @Z9, @WorksWorker11, @WorksTeam3, 1, @WorksSup1, 1, 0, 1, 31.910, 35.211, 150, DATEADD(DAY, 3, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE());

-- Agriculture team tasks
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, TeamId, IsTeamTask, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, IsSynced, SyncVersion, CreatedAt)
VALUES
(@MunicipalityId, N'تقليم أشجار حديقة حرب', N'تقليم وتنظيف الأشجار الكبيرة', @Z3, @AgriWorker1, @AgriTeam1, 1, @AgriSup1, 1, 0, 1, 31.910, 35.212, 200, DATEADD(DAY, 2, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'زراعة أشجار في المركز', N'زراعة 15 شجرة زيتون', @Z13, @AgriWorker1, @AgriTeam2, 1, @AgriSup1, 1, 0, 1, 31.911, 35.207, 200, DATEADD(DAY, 5, GETUTCDATE()), GETUTCDATE(), 1, 1, GETUTCDATE());

PRINT 'Created sample tasks';

-- ========================================================================
-- 7. SAMPLE ISSUES (Complaints)
-- ========================================================================
INSERT INTO [Issues] (MunicipalityId, Title, Description, ReportedByUserId, ZoneId, Type, Severity, Status, Latitude, Longitude, EventTime, IsSynced, SyncVersion, ReportedAt)
VALUES
(@MunicipalityId, N'تراكم نفايات بجانب المدرسة', N'شكوى من سكان المنطقة - نفايات متراكمة منذ 3 أيام', @HealthWorker1, @Z5, 0, 2, 0, 31.907, 35.214, GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'عمود إنارة مكسور', N'عمود إنارة ساقط في الشارع يشكل خطر', @WorksWorker1, @Z17, 1, 2, 1, 31.903, 35.205, GETUTCDATE(), 1, 1, GETUTCDATE()),
(@MunicipalityId, N'شجرة سقطت تسد الطريق', N'شجرة كبيرة سقطت بسبب الرياح', @AgriWorker1, @Z3, 2, 1, 0, 31.910, 35.213, GETUTCDATE(), 1, 1, GETUTCDATE());

PRINT 'Created sample issues';

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
PRINT 'USERS:';
PRINT '  Admin: 1';
PRINT '  Supervisors: 8 (5 Health + 2 Works + 1 Agri)';
PRINT '  Workers: 148 total';
PRINT '    - Health: 100 (individual, by zone)';
PRINT '    - Works: 30 (6 teams of 5)';
PRINT '    - Agriculture: 18 (5 teams of 3-4)';
PRINT '';
PRINT 'Teams: 11 (6 Works + 5 Agriculture)';
PRINT 'Zone Assignments: 100 (Health workers mapped to zones)';
PRINT '';
PRINT 'LOGIN CREDENTIALS (all passwords: pass):';
PRINT '  Admin: admin / pass';
PRINT '  Supervisors: super1..super8 / pass';
PRINT '    super1-5 = Health, super6-7 = Works, super8 = Agri';
PRINT '  Workers: worker1..worker148 / pass';
PRINT '    worker1-100 = Health, worker101-130 = Works, worker131-148 = Agri';
PRINT '========================================';
