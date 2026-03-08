-- ========================================================================
-- FOLLOWUP SYSTEM - AL-BIREH MUNICIPALITY REALISTIC SEED DATA
-- Demo accounts: admin, super1, super2, worker1, worker2, worker3
-- worker1+worker2 under super1 (NEW tasks only)
-- worker3 under super2 (VARIETY of statuses)
-- BZU zone added and assigned to demo workers
-- ========================================================================
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET DATEFIRST 7; -- Sunday=1, Friday=6, Saturday=7

USE FollowUpNew;

-- Demo date: March 8, 2026 at 09:00 UTC (11:00 Palestine time)
DECLARE @Now DATETIME = '2026-03-08 09:00:00';

-- Clean existing data (order matters: children before parents)
-- Break self-referencing / circular FKs first
UPDATE [Users] SET SupervisorId = NULL;
UPDATE [Tasks] SET SourceIssueId = NULL;

DELETE FROM [Photos];
DELETE FROM [Notifications];
DELETE FROM [Appeals];
DELETE FROM [AuditLogs];
DELETE FROM [LocationHistories];
DELETE FROM [TaskTemplates];
DELETE FROM [GisFiles];
DELETE FROM [TwoFactorCodes];
DELETE FROM [RefreshTokens];
DELETE FROM [Tasks];
DELETE FROM [Issues];
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
DBCC CHECKIDENT ('[Attendances]', RESEED, 0);
DBCC CHECKIDENT ('[Notifications]', RESEED, 0);
DBCC CHECKIDENT ('[Appeals]', RESEED, 0);
DBCC CHECKIDENT ('[AuditLogs]', RESEED, 0);
DBCC CHECKIDENT ('[LocationHistories]', RESEED, 0);
DBCC CHECKIDENT ('[Photos]', RESEED, 0);
DBCC CHECKIDENT ('[TaskTemplates]', RESEED, 0);
DBCC CHECKIDENT ('[GisFiles]', RESEED, 0);
DBCC CHECKIDENT ('[RefreshTokens]', RESEED, 0);
DBCC CHECKIDENT ('[TwoFactorCodes]', RESEED, 0);

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
    'info@al-bireh.ps', '+970-2-2404737', N'البيرة، فلسطين',
    31.88, 31.96, 35.17, 35.25,
    '07:00:00', '15:00:00', 15, 150.0, 1, @Now
);
DECLARE @MunicipalityId INT = SCOPE_IDENTITY();

-- ========================================================================
-- 2. DEPARTMENTS
-- ========================================================================
INSERT INTO [Departments] (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'دائرة الصحة والنظافة', 'Health & Sanitation', 'HEALTH', N'مسؤولة عن نظافة الشوارع وجمع النفايات - 100 عامل', 1, @Now);
DECLARE @HealthDeptId INT = SCOPE_IDENTITY();

INSERT INTO [Departments] (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'دائرة الأشغال العامة', 'Public Works', 'WORKS', N'مسؤولة عن الصيانة والبنية التحتية - 30 عامل في 6 فرق', 1, @Now);
DECLARE @WorksDeptId INT = SCOPE_IDENTITY();

INSERT INTO [Departments] (MunicipalityId, Name, NameEnglish, Code, Description, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'دائرة الزراعة', 'Agriculture', 'AGRI', N'مسؤولة عن الحدائق والمساحات الخضراء - 18 عامل في 5 فرق', 1, @Now);
DECLARE @AgriDeptId INT = SCOPE_IDENTITY();

-- ========================================================================
-- 3. ZONES (20 Real Al-Bireh Neighborhoods + BZU)
-- ========================================================================
INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البصبوص', '19', 'Al-Basbous', 31.896, 35.208, 125993, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z1 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'راس الطاحونة', '26', 'Ras-Attahouneh', 31.907, 35.212, 104253, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z2 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'حديقة حرب', '27', 'Hadiqat-Harb', 31.910, 35.212, 120466, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z3 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'قطعة شيبان', '5', 'Qitat-Shayban', 31.912, 35.217, 110971, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z4 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البلدية', '7', 'Al-Baladiyya', 31.907, 35.215, 54385, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z5 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البص', '11', 'Al-Bass', 31.899, 35.213, 106870, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z6 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'راس حسين', '21', 'Ras-Hsein', 31.897, 35.207, 131550, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z7 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الشيح الجنوبي', '15', 'AsSheikh-AlJanubi', 31.889, 35.215, 147098, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z8 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الغربية', '28', 'Al-Gharbieh', 31.910, 35.211, 121813, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z9 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'بئر الراس', '2', 'Bir-ArRas', 31.916, 35.217, 148286, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z10 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الميدان', '22', 'Al-Midan', 31.900, 35.209, 134508, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z11 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'سهل عواد', '4', 'Sahl-Awwad', 31.911, 35.218, 85603, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z12 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'المركز', '29', 'Al-Markaz', 31.911, 35.207, 103523, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z13 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الشيح الشمالي', '14', 'Al-Sheikh-AsShamali', 31.892, 35.214, 168505, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z14 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الجور التحتا', '3', 'Al-Jjuwar-AtTahta', 31.914, 35.219, 87603, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z15 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'البلدة القديمة', '331', 'Old-City', 31.905, 35.216, 84034, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z16 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'المنارة', '23', 'Al-Manara', 31.903, 35.206, 91684, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z17 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'الحومة', '10', 'Al-Homa', 31.900, 35.215, 65365, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z18 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'المبارخ', '18', 'Al-Mbarekh', 31.892, 35.208, 102778, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z19 INT = SCOPE_IDENTITY();

INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'القبارصة', '1', 'Al-Qabarsah', 31.916, 35.220, 113248, N'البيرة', 0, 1, @Now, 1, @Now);
DECLARE @Z20 INT = SCOPE_IDENTITY();

-- Zone 21: BZU (Birzeit University)
INSERT INTO [Zones] (MunicipalityId, ZoneName, ZoneCode, Description, CenterLatitude, CenterLongitude, AreaSquareMeters, District, ZoneType, Version, VersionDate, IsActive, CreatedAt)
VALUES (@MunicipalityId, N'جامعة بيرزيت', 'BZU', 'Birzeit University', 31.9554, 35.1751, 250000, N'بيرزيت', 0, 1, @Now, 1, @Now);
DECLARE @ZBZU INT = SCOPE_IDENTITY();

PRINT 'Created 21 zones (20 Al-Bireh + BZU)';

-- ========================================================================
-- 3b. ZONE BOUNDARIES (auto-generate irregular polygons from center + area)
-- At lat ~31.9: 1° lat ≈ 111,000m, 1° lng ≈ 111,000*cos(31.9°) ≈ 94,239m
-- ========================================================================
DECLARE @cLat FLOAT, @cLng FLOAT, @area FLOAT, @halfH FLOAT, @halfW FLOAT;
DECLARE @geoJson NVARCHAR(MAX), @wkt NVARCHAR(MAX);
DECLARE @zoneVar INT, @zIdx INT = 1;

-- Temp table to iterate zones
DECLARE @ZoneBounds TABLE (idx INT IDENTITY(1,1), zid INT, clat FLOAT, clng FLOAT, area FLOAT);
INSERT INTO @ZoneBounds (zid, clat, clng, area) VALUES
(@Z1,  31.896, 35.208, 125993), (@Z2,  31.907, 35.212, 104253),
(@Z3,  31.910, 35.212, 120466), (@Z4,  31.912, 35.217, 110971),
(@Z5,  31.907, 35.215, 54385),  (@Z6,  31.899, 35.213, 106870),
(@Z7,  31.897, 35.207, 131550), (@Z8,  31.889, 35.215, 147098),
(@Z9,  31.910, 35.211, 121813), (@Z10, 31.916, 35.217, 148286),
(@Z11, 31.900, 35.209, 134508), (@Z12, 31.911, 35.218, 85603),
(@Z13, 31.911, 35.207, 103523), (@Z14, 31.892, 35.214, 168505),
(@Z15, 31.914, 35.219, 87603),  (@Z16, 31.905, 35.216, 84034),
(@Z17, 31.903, 35.206, 91684),  (@Z18, 31.900, 35.215, 65365),
(@Z19, 31.892, 35.208, 102778), (@Z20, 31.916, 35.220, 113248),
(@ZBZU, 31.9554, 35.1751, 250000);

WHILE @zIdx <= 21
BEGIN
    SELECT @zoneVar = zid, @cLat = clat, @cLng = clng, @area = area
    FROM @ZoneBounds WHERE idx = @zIdx;

    -- Half-extents in degrees (slightly rectangular, not square)
    SET @halfH = SQRT(@area) / 111000.0 * 0.55;  -- taller
    SET @halfW = SQRT(@area) / 94239.0 * 0.48;   -- narrower

    -- 6-point irregular polygon (GeoJSON is [lng,lat])
    SET @geoJson = '{"type":"Polygon","coordinates":[[' +
        '[' + CAST(ROUND(@cLng - @halfW * 0.90, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat + @halfH * 0.80, 6) AS VARCHAR) + '],' +
        '[' + CAST(ROUND(@cLng + @halfW * 0.25, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat + @halfH * 1.00, 6) AS VARCHAR) + '],' +
        '[' + CAST(ROUND(@cLng + @halfW * 0.95, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat + @halfH * 0.55, 6) AS VARCHAR) + '],' +
        '[' + CAST(ROUND(@cLng + @halfW * 0.85, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat - @halfH * 0.75, 6) AS VARCHAR) + '],' +
        '[' + CAST(ROUND(@cLng - @halfW * 0.20, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat - @halfH * 1.00, 6) AS VARCHAR) + '],' +
        '[' + CAST(ROUND(@cLng - @halfW * 0.95, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat - @halfH * 0.50, 6) AS VARCHAR) + '],' +
        '[' + CAST(ROUND(@cLng - @halfW * 0.90, 6) AS VARCHAR) + ',' + CAST(ROUND(@cLat + @halfH * 0.80, 6) AS VARCHAR) + ']' +
        ']]}';

    -- WKT for geography (WKT is lng lat, counter-clockwise for geography)
    SET @wkt = 'POLYGON((' +
        CAST(ROUND(@cLng - @halfW * 0.90, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat + @halfH * 0.80, 6) AS VARCHAR) + ',' +
        CAST(ROUND(@cLng - @halfW * 0.95, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat - @halfH * 0.50, 6) AS VARCHAR) + ',' +
        CAST(ROUND(@cLng - @halfW * 0.20, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat - @halfH * 1.00, 6) AS VARCHAR) + ',' +
        CAST(ROUND(@cLng + @halfW * 0.85, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat - @halfH * 0.75, 6) AS VARCHAR) + ',' +
        CAST(ROUND(@cLng + @halfW * 0.95, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat + @halfH * 0.55, 6) AS VARCHAR) + ',' +
        CAST(ROUND(@cLng + @halfW * 0.25, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat + @halfH * 1.00, 6) AS VARCHAR) + ',' +
        CAST(ROUND(@cLng - @halfW * 0.90, 6) AS VARCHAR) + ' ' + CAST(ROUND(@cLat + @halfH * 0.80, 6) AS VARCHAR) +
        '))';

    UPDATE [Zones]
    SET BoundaryGeoJson = @geoJson,
        Boundary = geography::STGeomFromText(@wkt, 4326)
    WHERE ZoneId = @zoneVar;

    SET @zIdx = @zIdx + 1;
END;

PRINT 'Generated boundaries for 21 zones';

-- ========================================================================
-- 4. TEAMS (6 Works + 5 Agriculture)
-- ========================================================================
INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق صيانة الطرق', 'ROAD1', 'Road Maintenance Team 1', 5, 1, @Now);
DECLARE @WorksTeam1 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق صيانة الشبكات', 'INFRA1', 'Infrastructure Team', 5, 1, @Now);
DECLARE @WorksTeam2 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق الإنارة', 'LIGHT1', 'Lighting Team', 5, 1, @Now);
DECLARE @WorksTeam3 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق الطوارئ', 'EMERG1', 'Emergency Response Team', 5, 1, @Now);
DECLARE @WorksTeam4 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق البناء', 'BUILD1', 'Construction Team', 5, 1, @Now);
DECLARE @WorksTeam5 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@WorksDeptId, N'فريق الصيانة العامة', 'MAINT1', 'General Maintenance', 5, 1, @Now);
DECLARE @WorksTeam6 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق الحدائق العامة', 'PARK1', 'Public Parks Team', 4, 1, @Now);
DECLARE @AgriTeam1 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق التشجير', 'TREE1', 'Tree Planting Team', 4, 1, @Now);
DECLARE @AgriTeam2 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق الري', 'IRRIG1', 'Irrigation Team', 4, 1, @Now);
DECLARE @AgriTeam3 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق الصيانة الزراعية', 'AGMNT1', 'Agricultural Maintenance', 3, 1, @Now);
DECLARE @AgriTeam4 INT = SCOPE_IDENTITY();

INSERT INTO [Teams] (DepartmentId, Name, Code, Description, MaxMembers, IsActive, CreatedAt)
VALUES (@AgriDeptId, N'فريق المشاتل', 'NURSE1', 'Nursery Team', 3, 1, @Now);
DECLARE @AgriTeam5 INT = SCOPE_IDENTITY();

PRINT 'Created 11 teams (6 Works + 5 Agriculture)';

-- ========================================================================
-- 5. USERS
-- ========================================================================
DECLARE @PassHash NVARCHAR(MAX) = 'AQAAAAIAAYagAAAAEIXrt9dNRaqI6aSHYa1Spfy/Pj8b493OqrJAL+IuHLZrFauRlWstpQIh5ZRQzfwNhw==';

-- 5.1 ADMIN
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'admin', @PassHash, 'admin@albireh.ps', '+970599000001', N'مدير النظام', 0, NULL, NULL, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @AdminId INT = SCOPE_IDENTITY();

-- 5.2 SUPERVISORS
-- Health Supervisors
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super1', @PassHash, 'health1@albireh.ps', '+970599010001', N'أحمد محمد - مراقب صحة 1', 1, NULL, @HealthDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @HealthSup1 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super2', @PassHash, 'health2@albireh.ps', '+970599010002', N'خالد علي - مراقب صحة 2', 1, NULL, @HealthDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @HealthSup2 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super3', @PassHash, 'health3@albireh.ps', '+970599010003', N'سامي حسن - مراقب صحة 3', 1, NULL, @HealthDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @HealthSup3 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super4', @PassHash, 'health4@albireh.ps', '+970599010004', N'محمود عمر - مراقب صحة 4', 1, NULL, @HealthDeptId, NULL, NULL, 1, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL); -- Inactive
DECLARE @HealthSup4 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super5', @PassHash, 'health5@albireh.ps', '+970599010005', N'يوسف سعيد - مراقب صحة 5', 1, NULL, @HealthDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @HealthSup5 INT = SCOPE_IDENTITY();

-- Works Supervisors
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super6', @PassHash, 'works1@albireh.ps', '+970599020001', N'طارق نبيل - مراقب أشغال 1', 1, NULL, @WorksDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @WorksSup1 INT = SCOPE_IDENTITY();

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super7', @PassHash, 'works2@albireh.ps', '+970599020002', N'فادي جمال - مراقب أشغال 2', 1, NULL, @WorksDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @WorksSup2 INT = SCOPE_IDENTITY();

-- Agriculture Supervisor
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES (@MunicipalityId, 'super8', @PassHash, 'agri1@albireh.ps', '+970599030001', N'وليد حسين - مراقب زراعة', 1, NULL, @AgriDeptId, NULL, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);
DECLARE @AgriSup1 INT = SCOPE_IDENTITY();

PRINT 'Created 8 supervisors';

-- ========================================================================
-- 5.3 HEALTH WORKERS (100 workers via WHILE loop)
-- ========================================================================
DECLARE @i INT = 1;
DECLARE @ZoneId INT;
DECLARE @SupervisorId INT;
DECLARE @WorkerName NVARCHAR(100);

DECLARE @HealthNames TABLE (idx INT IDENTITY(1,1), Name NVARCHAR(100));
INSERT INTO @HealthNames (Name) VALUES
(N'محمد عبد الله'),(N'أحمد الرشيد'),(N'خالد البدوي'),(N'عمر نصار'),(N'سامي عوض'),
(N'يوسف برغوث'),(N'إبراهيم زيدان'),(N'حسن حمدان'),(N'طارق الأحمد'),(N'فادي الخليل'),
(N'ياسر شحادة'),(N'شادي عيسى'),(N'أسامة سلامة'),(N'مهند قاسم'),(N'قصي رضوان'),
(N'براء خليل'),(N'أنس درويش'),(N'حمزة الصالح'),(N'عبدالله حداد'),(N'إياد منصور'),
(N'كرم الحسيني'),(N'بشار شهاوي'),(N'غسان جبران'),(N'صهيب النجار'),(N'مصعب الخضر'),
(N'رامي طوقان'),(N'ماجد عبد الغني'),(N'هشام أبو زيد'),(N'نادر سعدات'),(N'زياد الزبير'),
(N'باسم فارس'),(N'عماد بركات'),(N'سامر نصار'),(N'أيمن عوض'),(N'حسام برغوث'),
(N'فراس زيدان'),(N'معتز حمدان'),(N'ثائر الأحمد'),(N'وسام الخليل'),(N'حازم شحادة'),
(N'ربيع عيسى'),(N'لقمان سلامة'),(N'مصطفى قاسم'),(N'نائل رضوان'),(N'ياسين خليل'),
(N'نعمان درويش'),(N'صالح النجار'),(N'بلال حداد'),(N'صلاح منصور'),(N'حسن الحسيني'),
(N'تامر شهاوي'),(N'أمير جبران'),(N'وليد النجار'),(N'محمود الخضر'),(N'نزار طوقان'),
(N'عمار عبد الغني'),(N'سلامة أبو زيد'),(N'مازن سعدات'),(N'سعد الزبير'),(N'حمدي فارس'),
(N'عزت بركات'),(N'رفيق نصار'),(N'توفيق عوض'),(N'شريف برغوث'),(N'مجدي زيدان'),
(N'رضا حمدان'),(N'عارف الأحمد'),(N'زكريا الخليل'),(N'عبد الرحمن شحادة'),(N'راشد عيسى'),
(N'رائد سلامة'),(N'نضال قاسم'),(N'جهاد رضوان'),(N'عصام خليل'),(N'هيثم درويش'),
(N'لؤي الصالح'),(N'رشيد حداد'),(N'حاتم منصور'),(N'مأمون الحسيني'),(N'صابر شهاوي'),
(N'ناصر جبران'),(N'سهيل النجار'),(N'عادل الخضر'),(N'فيصل طوقان'),(N'طلال عبد الغني'),
(N'وائل أبو زيد'),(N'خليل سعدات'),(N'منير الزبير'),(N'جمال فارس'),(N'عمر بركات'),
(N'صالح نصار'),(N'يحيى عوض'),(N'مروان برغوث'),(N'إسماعيل زيدان'),(N'نبيل حمدان'),
(N'كمال الأحمد'),(N'سعيد الخليل'),(N'علاء شحادة'),(N'خضر عيسى'),(N'تيسير سلامة');

DECLARE @ZoneIds TABLE (idx INT IDENTITY(1,1), ZoneId INT);
INSERT INTO @ZoneIds (ZoneId) VALUES (@Z1),(@Z2),(@Z3),(@Z4),(@Z5),(@Z6),(@Z7),(@Z8),(@Z9),(@Z10),
                                     (@Z11),(@Z12),(@Z13),(@Z14),(@Z15),(@Z16),(@Z17),(@Z18),(@Z19),(@Z20);

WHILE @i <= 100
BEGIN
    SET @SupervisorId = CASE
        WHEN @i <= 20 THEN @HealthSup1
        WHEN @i <= 40 THEN @HealthSup2
        WHEN @i <= 60 THEN @HealthSup3
        WHEN @i <= 80 THEN @HealthSup4
        ELSE @HealthSup5
    END;
    SELECT @ZoneId = ZoneId FROM @ZoneIds WHERE idx = ((@i - 1) / 5) + 1;
    SELECT @WorkerName = Name FROM @HealthNames WHERE idx = @i;

    INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
    VALUES (@MunicipalityId, 'worker' + CAST(@i AS VARCHAR(10)), @PassHash, NULL,
            '+97059910' + RIGHT('0000' + CAST(@i AS VARCHAR(4)), 4), @WorkerName,
            2, 0, @HealthDeptId, @SupervisorId, NULL, 0, @Now, NULL, NULL, 0, '07:00:00', '15:00:00', 15, NULL);

    SET @i = @i + 1;
END;

-- IMPORTANT: Reassign worker3 to super2
UPDATE [Users] SET SupervisorId = @HealthSup2 WHERE Username = 'worker3';

PRINT 'Created 100 health workers (worker3 reassigned to super2)';

-- ========================================================================
-- 5.4 WORKS WORKERS (30 workers in 6 teams of 5)
-- ========================================================================
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker101',@PassHash,'worker101@albireh.ps','+970599200001',N'رامي طارق',2,1,@WorksDeptId,@WorksSup1,@WorksTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker102',@PassHash,'worker102@albireh.ps','+970599200002',N'ماجد سليم',2,1,@WorksDeptId,@WorksSup1,@WorksTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker103',@PassHash,'worker103@albireh.ps','+970599200003',N'هشام كمال',2,1,@WorksDeptId,@WorksSup1,@WorksTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker104',@PassHash,'worker104@albireh.ps','+970599200004',N'نادر جمال',2,1,@WorksDeptId,@WorksSup1,@WorksTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker105',@PassHash,'worker105@albireh.ps','+970599200005',N'زياد محمود',2,1,@WorksDeptId,@WorksSup1,@WorksTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker106',@PassHash,'worker106@albireh.ps','+970599200006',N'باسم عادل',2,1,@WorksDeptId,@WorksSup1,@WorksTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker107',@PassHash,'worker107@albireh.ps','+970599200007',N'عماد خالد',2,1,@WorksDeptId,@WorksSup1,@WorksTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker108',@PassHash,'worker108@albireh.ps','+970599200008',N'سامر فيصل',2,1,@WorksDeptId,@WorksSup1,@WorksTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker109',@PassHash,'worker109@albireh.ps','+970599200009',N'أيمن راشد',2,1,@WorksDeptId,@WorksSup1,@WorksTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker110',@PassHash,'worker110@albireh.ps','+970599200010',N'حسام وليد',2,1,@WorksDeptId,@WorksSup1,@WorksTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker111',@PassHash,'worker111@albireh.ps','+970599200011',N'فراس أحمد',2,1,@WorksDeptId,@WorksSup1,@WorksTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker112',@PassHash,'worker112@albireh.ps','+970599200012',N'معتز علي',2,1,@WorksDeptId,@WorksSup1,@WorksTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker113',@PassHash,'worker113@albireh.ps','+970599200013',N'ثائر محمد',2,1,@WorksDeptId,@WorksSup1,@WorksTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker114',@PassHash,'worker114@albireh.ps','+970599200014',N'وسام سعيد',2,1,@WorksDeptId,@WorksSup1,@WorksTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker115',@PassHash,'worker115@albireh.ps','+970599200015',N'حازم نبيل',2,1,@WorksDeptId,@WorksSup1,@WorksTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker116',@PassHash,'worker116@albireh.ps','+970599200016',N'ياسر حسن',2,1,@WorksDeptId,@WorksSup2,@WorksTeam4,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker117',@PassHash,'worker117@albireh.ps','+970599200017',N'شادي عمر',2,1,@WorksDeptId,@WorksSup2,@WorksTeam4,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker118',@PassHash,'worker118@albireh.ps','+970599200018',N'أسامة طلال',2,1,@WorksDeptId,@WorksSup2,@WorksTeam4,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker119',@PassHash,'worker119@albireh.ps','+970599200019',N'مهند فادي',2,1,@WorksDeptId,@WorksSup2,@WorksTeam4,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker120',@PassHash,'worker120@albireh.ps','+970599200020',N'قصي رامي',2,1,@WorksDeptId,@WorksSup2,@WorksTeam4,1,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL); -- Inactive

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker121',@PassHash,'worker121@albireh.ps','+970599200021',N'براء خالد',2,1,@WorksDeptId,@WorksSup2,@WorksTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker122',@PassHash,'worker122@albireh.ps','+970599200022',N'أنس محمود',2,1,@WorksDeptId,@WorksSup2,@WorksTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker123',@PassHash,'worker123@albireh.ps','+970599200023',N'حمزة سامي',2,1,@WorksDeptId,@WorksSup2,@WorksTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker124',@PassHash,'worker124@albireh.ps','+970599200024',N'عبدالله يوسف',2,1,@WorksDeptId,@WorksSup2,@WorksTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker125',@PassHash,'worker125@albireh.ps','+970599200025',N'إياد ماهر',2,1,@WorksDeptId,@WorksSup2,@WorksTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker126',@PassHash,'worker126@albireh.ps','+970599200026',N'كرم طارق',2,1,@WorksDeptId,@WorksSup2,@WorksTeam6,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker127',@PassHash,'worker127@albireh.ps','+970599200027',N'بشار عادل',2,1,@WorksDeptId,@WorksSup2,@WorksTeam6,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker128',@PassHash,'worker128@albireh.ps','+970599200028',N'غسان فيصل',2,1,@WorksDeptId,@WorksSup2,@WorksTeam6,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker129',@PassHash,'worker129@albireh.ps','+970599200029',N'صهيب راشد',2,1,@WorksDeptId,@WorksSup2,@WorksTeam6,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker130',@PassHash,'worker130@albireh.ps','+970599200030',N'مصعب وليد',2,1,@WorksDeptId,@WorksSup2,@WorksTeam6,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

PRINT 'Created 30 works workers in 6 teams';

-- ========================================================================
-- 5.5 AGRICULTURE WORKERS (18 workers in 5 teams)
-- ========================================================================
INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker131',@PassHash,'worker131@albireh.ps','+970599300001',N'منير أحمد',2,2,@AgriDeptId,@AgriSup1,@AgriTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker132',@PassHash,'worker132@albireh.ps','+970599300002',N'رائد محمد',2,2,@AgriDeptId,@AgriSup1,@AgriTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker133',@PassHash,'worker133@albireh.ps','+970599300003',N'نضال علي',2,2,@AgriDeptId,@AgriSup1,@AgriTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker134',@PassHash,'worker134@albireh.ps','+970599300004',N'جهاد خالد',2,2,@AgriDeptId,@AgriSup1,@AgriTeam1,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker135',@PassHash,'worker135@albireh.ps','+970599300005',N'عصام سامي',2,2,@AgriDeptId,@AgriSup1,@AgriTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker136',@PassHash,'worker136@albireh.ps','+970599300006',N'هيثم نبيل',2,2,@AgriDeptId,@AgriSup1,@AgriTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker137',@PassHash,'worker137@albireh.ps','+970599300007',N'لؤي طارق',2,2,@AgriDeptId,@AgriSup1,@AgriTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker138',@PassHash,'worker138@albireh.ps','+970599300008',N'رشيد عمر',2,2,@AgriDeptId,@AgriSup1,@AgriTeam2,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker139',@PassHash,'worker139@albireh.ps','+970599300009',N'حاتم فادي',2,2,@AgriDeptId,@AgriSup1,@AgriTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker140',@PassHash,'worker140@albireh.ps','+970599300010',N'مأمون رامي',2,2,@AgriDeptId,@AgriSup1,@AgriTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker141',@PassHash,'worker141@albireh.ps','+970599300011',N'صابر حسن',2,2,@AgriDeptId,@AgriSup1,@AgriTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker142',@PassHash,'worker142@albireh.ps','+970599300012',N'ناصر جمال',2,2,@AgriDeptId,@AgriSup1,@AgriTeam3,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker143',@PassHash,'worker143@albireh.ps','+970599300013',N'سهيل محمود',2,2,@AgriDeptId,@AgriSup1,@AgriTeam4,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker144',@PassHash,'worker144@albireh.ps','+970599300014',N'عادل سعيد',2,2,@AgriDeptId,@AgriSup1,@AgriTeam4,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker145',@PassHash,'worker145@albireh.ps','+970599300015',N'فيصل أحمد',2,2,@AgriDeptId,@AgriSup1,@AgriTeam4,2,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL); -- Suspended

INSERT INTO [Users] (MunicipalityId, Username, PasswordHash, Email, PhoneNumber, FullName, Role, WorkerType, DepartmentId, SupervisorId, TeamId, Status, CreatedAt, LastLoginAt, LastBatteryLevel, IsLowBattery, ExpectedStartTime, ExpectedEndTime, GraceMinutes, ProfilePhotoUrl)
VALUES
(@MunicipalityId,'worker146',@PassHash,'worker146@albireh.ps','+970599300016',N'طلال كمال',2,2,@AgriDeptId,@AgriSup1,@AgriTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker147',@PassHash,'worker147@albireh.ps','+970599300017',N'وائل نادر',2,2,@AgriDeptId,@AgriSup1,@AgriTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL),
(@MunicipalityId,'worker148',@PassHash,'worker148@albireh.ps','+970599300018',N'خليل ماجد',2,2,@AgriDeptId,@AgriSup1,@AgriTeam5,0,@Now,NULL,NULL,0,'07:00:00','15:00:00',15,NULL);

PRINT 'Created 18 agriculture workers in 5 teams';

-- ========================================================================
-- 5.6 ZONE ASSIGNMENTS
-- ========================================================================
-- Health workers zone loop
DECLARE @uzIdx INT = 1;
DECLARE @uzUserId INT;
DECLARE @uzZoneId INT;
DECLARE @uzSupervisorId INT;

WHILE @uzIdx <= 100
BEGIN
    SELECT @uzUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@uzIdx AS VARCHAR(10));
    SELECT @uzZoneId = ZoneId FROM @ZoneIds WHERE idx = ((@uzIdx - 1) / 5) + 1;
    SET @uzSupervisorId = CASE
        WHEN @uzIdx <= 20 THEN @HealthSup1
        WHEN @uzIdx <= 40 THEN @HealthSup2
        WHEN @uzIdx <= 60 THEN @HealthSup3
        WHEN @uzIdx <= 80 THEN @HealthSup4
        ELSE @HealthSup5
    END;
    INSERT INTO [UserZones] (UserId, ZoneId, AssignedAt, AssignedByUserId, IsActive)
    VALUES (@uzUserId, @uzZoneId, @Now, @uzSupervisorId, 1);
    SET @uzIdx = @uzIdx + 1;
END;

-- Resolve demo worker IDs
DECLARE @W1 INT, @W2 INT, @W3 INT;
SELECT @W1 = UserId FROM [Users] WHERE Username = 'worker1';
SELECT @W2 = UserId FROM [Users] WHERE Username = 'worker2';
SELECT @W3 = UserId FROM [Users] WHERE Username = 'worker3';

-- Add BZU zone to demo workers
INSERT INTO [UserZones] (UserId, ZoneId, AssignedAt, AssignedByUserId, IsActive) VALUES
(@W1, @ZBZU, @Now, @HealthSup1, 1),
(@W2, @ZBZU, @Now, @HealthSup1, 1),
(@W3, @ZBZU, @Now, @HealthSup2, 1);

PRINT 'Created zone assignments (100 health workers + BZU for demo workers)';

-- 5.7 Update demo users to be active NOW
UPDATE [Users] SET LastLoginAt = @Now, LastBatteryLevel = 85, IsLowBattery = 0 WHERE Username = 'admin';
UPDATE [Users] SET LastLoginAt = @Now, LastBatteryLevel = 72, IsLowBattery = 0 WHERE Username = 'super1';
UPDATE [Users] SET LastLoginAt = @Now, LastBatteryLevel = 65, IsLowBattery = 0 WHERE Username = 'super2';
UPDATE [Users] SET LastLoginAt = @Now, LastBatteryLevel = 78, IsLowBattery = 0 WHERE Username = 'worker1';
UPDATE [Users] SET LastLoginAt = @Now, LastBatteryLevel = 45, IsLowBattery = 0 WHERE Username = 'worker2';
UPDATE [Users] SET LastLoginAt = @Now, LastBatteryLevel = 91, IsLowBattery = 0 WHERE Username = 'worker3';

PRINT 'Demo users updated as active';

-- ========================================================================
-- 6. TASKS
-- ========================================================================

-- -----------------------------------------------------------------------
-- worker1 (under super1, zones Z1 + BZU): 6 NEW Pending tasks only
-- -----------------------------------------------------------------------
-- W1-T1: GarbageCollection, Medium, due today, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات صباحي - البصبوص', N'جولة جمع نفايات صباحية شاملة في حي البصبوص', @Z1, @W1, @HealthSup1, 1, 0, 0, 31.8960, 35.2080, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W1-T2: StreetSweeping, Medium, due today, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'كنس شوارع فرعية - البصبوص', N'كنس وتنظيف الشوارع الفرعية في حي البصبوص', @Z1, @W1, @HealthSup1, 1, 0, 1, 31.8963, 35.2082, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W1-T3: ContainerMaintenance, High, due today, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'صيانة حاويات شارع 5', N'فحص وإصلاح الحاويات التالفة في شارع 5 بالبصبوص', @Z1, @W1, @HealthSup1, 2, 0, 2, 31.8955, 35.2075, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W1-T4: PublicSpaceCleaning, Low, due tomorrow, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'تنظيف ساحة البصبوص', N'تنظيف شامل للساحة العامة في حي البصبوص وإزالة الأعشاب', @Z1, @W1, @HealthSup1, 0, 0, 4, 31.8962, 35.2081, 50, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- W1-T5: Inspection, High, due tomorrow, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جولة تفتيشية - البصبوص', N'جولة تفتيش عامة على نظافة ومرافق منطقة البصبوص', @Z1, @W1, @HealthSup1, 2, 0, 5, 31.8965, 35.2085, 50, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- W1-T6: GarbageCollection at BZU, Medium, due tomorrow
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - جامعة بيرزيت', N'جمع النفايات من الحاويات في محيط جامعة بيرزيت', @ZBZU, @W1, @HealthSup1, 1, 0, 0, 31.9554, 35.1751, 100, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- -----------------------------------------------------------------------
-- worker2 (under super1, zones Z1 + BZU): 6 NEW Pending tasks only
-- -----------------------------------------------------------------------
-- W2-T1: GarbageCollection, Medium, due today, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - البصبوص شمال', N'جمع النفايات من الجزء الشمالي لحي البصبوص', @Z1, @W2, @HealthSup1, 1, 0, 0, 31.8968, 35.2088, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W2-T2: PublicSpaceCleaning, High, due today, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'تنظيف محيط المدرسة', N'تنظيف الساحة والممرات المحيطة بمدرسة البصبوص', @Z1, @W2, @HealthSup1, 2, 0, 4, 31.8957, 35.2079, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W2-T3: ContainerMaintenance, Medium, due today, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'صيانة حاويات الحي الغربي', N'فحص وتنظيف حاويات الجزء الغربي من البصبوص', @Z1, @W2, @HealthSup1, 1, 0, 2, 31.8953, 35.2077, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W2-T4: StreetSweeping, Low, due tomorrow, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'كنس الشارع الرئيسي', N'كنس وتنظيف الشارع الرئيسي في حي البصبوص', @Z1, @W2, @HealthSup1, 0, 0, 1, 31.8966, 35.2086, 50, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- W2-T5: GarbageCollection (recycling), Medium, due tomorrow, zone Z1
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'فرز نفايات قابلة للتدوير', N'جمع وفرز النفايات القابلة للتدوير من الحاويات الخضراء', @Z1, @W2, @HealthSup1, 1, 0, 0, 31.8959, 35.2083, 50, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- W2-T6: PublicSpaceCleaning at BZU, Medium, due tomorrow
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'تنظيف ساحة جامعة بيرزيت', N'تنظيف الساحات والممرات الخارجية لجامعة بيرزيت', @ZBZU, @W2, @HealthSup1, 1, 0, 4, 31.9554, 35.1751, 100, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- -----------------------------------------------------------------------
-- worker3 (under super2, zones Z2 + BZU): VARIETY of statuses (7 tasks)
-- -----------------------------------------------------------------------
-- W3-T1: Completed+Approved 20 days ago, GarbageCollection
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - راس الطاحونة', N'جمع نفايات يومي من حاويات منطقة راس الطاحونة', @Z2, @W3, @HealthSup2, 1, 3, 0, 31.9065, 35.2115, 50, DATEADD(DAY,-20,@Now), DATEADD(DAY,-21,@Now), DATEADD(DAY,-20,@Now), DATEADD(DAY,-20,@Now), N'تم جمع جميع النفايات بنجاح', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 2, DATEADD(DAY,-21,@Now));

-- W3-T2: Completed+Approved 10 days ago, StreetSweeping
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'كنس شوارع راس الطاحونة', N'كنس وتنظيف الشوارع الرئيسية في راس الطاحونة', @Z2, @W3, @HealthSup2, 1, 3, 1, 31.9072, 35.2118, 50, DATEADD(DAY,-10,@Now), DATEADD(DAY,-11,@Now), DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now), N'تم كنس جميع الشوارع الرئيسية', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 2, DATEADD(DAY,-11,@Now));

-- W3-T3: UnderReview today, ContainerMaintenance
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'صيانة حاويات - راس الطاحونة', N'فحص وتنظيف الحاويات في منطقة راس الطاحونة', @Z2, @W3, @HealthSup2, 1, 2, 2, 31.9068, 35.2122, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), DATEADD(HOUR,-4,@Now), DATEADD(HOUR,-1,@Now), N'تم فحص وتنظيف جميع الحاويات', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W3-T4: Rejected by super2 manual, 5 days ago
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات مسائي - راس الطاحونة', N'جولة مسائية لجمع النفايات المتراكمة', @Z2, @W3, @HealthSup2, 1, 4, 0, 31.9070, 35.2116, 50, DATEADD(DAY,-5,@Now), DATEADD(DAY,-6,@Now), DATEADD(DAY,-5,@Now), DATEADD(DAY,-5,@Now), N'تم الجمع', 100, NULL, 0, N'الصور غير واضحة - يرجى إعادة التصوير', DATEADD(DAY,-5,@Now), @HealthSup2, NULL, NULL, NULL, 1, 1, NULL, NULL, 1, 2, DATEADD(DAY,-6,@Now));

-- W3-T5: InProgress 60%, started 2hrs ago, PublicSpaceCleaning
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'تنظيف ساحة راس الطاحونة', N'تنظيف الساحة العامة وإزالة المخلفات', @Z2, @W3, @HealthSup2, 1, 1, 4, 31.9073, 35.2120, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), DATEADD(HOUR,-2,@Now), NULL, NULL, 60, N'تم تنظيف أكثر من نصف الساحة', 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W3-T6: Pending, due today, Inspection
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جولة تفتيشية - راس الطاحونة', N'جولة تفتيش على نظافة ومرافق منطقة راس الطاحونة', @Z2, @W3, @HealthSup2, 1, 0, 5, 31.9067, 35.2119, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- W3-T7: Pending, due tomorrow, GarbageCollection at BZU
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - جامعة بيرزيت', N'جمع النفايات من الحاويات في محيط جامعة بيرزيت', @ZBZU, @W3, @HealthSup2, 1, 0, 0, 31.9554, 35.1751, 100, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- -----------------------------------------------------------------------
-- Other workers (worker4-worker10 under super1): ~10 historical tasks
-- -----------------------------------------------------------------------
DECLARE @W4 INT, @W5 INT, @W6 INT, @W7 INT, @W8 INT, @W9 INT, @W10 INT;
SELECT @W4 = UserId FROM [Users] WHERE Username = 'worker4';
SELECT @W5 = UserId FROM [Users] WHERE Username = 'worker5';
SELECT @W6 = UserId FROM [Users] WHERE Username = 'worker6';
SELECT @W7 = UserId FROM [Users] WHERE Username = 'worker7';
SELECT @W8 = UserId FROM [Users] WHERE Username = 'worker8';
SELECT @W9 = UserId FROM [Users] WHERE Username = 'worker9';
SELECT @W10 = UserId FROM [Users] WHERE Username = 'worker10';

-- 3 completed
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - البصبوص', N'جولة جمع نفايات يومية', @Z1, @W4, @HealthSup1, 1, 3, 0, 31.8960, 35.2080, 50, DATEADD(DAY,-15,@Now), DATEADD(DAY,-16,@Now), DATEADD(DAY,-15,@Now), DATEADD(DAY,-15,@Now), N'مكتمل', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 2, DATEADD(DAY,-16,@Now));

INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'كنس شوارع - حديقة حرب', N'كنس يومي للشوارع', @Z3, @W5, @HealthSup1, 1, 3, 1, 31.9100, 35.2120, 50, DATEADD(DAY,-12,@Now), DATEADD(DAY,-13,@Now), DATEADD(DAY,-12,@Now), DATEADD(DAY,-12,@Now), N'مكتمل', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 2, DATEADD(DAY,-13,@Now));

INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'صيانة حاويات - قطعة شيبان', N'فحص الحاويات', @Z4, @W6, @HealthSup1, 2, 3, 2, 31.9120, 35.2170, 50, DATEADD(DAY,-8,@Now), DATEADD(DAY,-9,@Now), DATEADD(DAY,-8,@Now), DATEADD(DAY,-8,@Now), N'مكتمل', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 2, DATEADD(DAY,-9,@Now));

-- 2 in-progress
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'تنظيف ساحة - البلدية', N'تنظيف الساحات العامة', @Z5, @W7, @HealthSup1, 1, 1, 4, 31.9070, 35.2150, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), DATEADD(HOUR,-3,@Now), NULL, NULL, 50, N'جاري العمل', 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - البص', N'جمع النفايات من منطقة البص', @Z6, @W8, @HealthSup1, 2, 1, 0, 31.8990, 35.2130, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), DATEADD(HOUR,-2,@Now), NULL, NULL, 30, N'في البداية', 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- 2 pending
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'كنس شوارع - راس حسين', N'كنس يومي للشوارع', @Z7, @W9, @HealthSup1, 0, 0, 1, 31.8970, 35.2070, 50, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'تفتيش - الشيح الجنوبي', N'جولة تفتيشية على النظافة', @Z8, @W10, @HealthSup1, 1, 0, 5, 31.8890, 35.2150, 50, DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

-- 1 rejected
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'جمع نفايات - الغربية', N'جمع النفايات اليومي', @Z9, @W4, @HealthSup1, 1, 4, 0, 31.9100, 35.2110, 50, DATEADD(DAY,-10,@Now), DATEADD(DAY,-11,@Now), DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now), N'تم', 100, NULL, 0, N'لم يتم تصوير جميع الحاويات', DATEADD(DAY,-10,@Now), @HealthSup1, NULL, NULL, NULL, 1, 1, NULL, NULL, 1, 2, DATEADD(DAY,-11,@Now));

-- 1 under-review
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'كنس - بئر الراس', N'كنس الشوارع الرئيسية', @Z10, @W5, @HealthSup1, 1, 2, 1, 31.9160, 35.2170, 50, CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), DATEADD(HOUR,-5,@Now), DATEADD(HOUR,-1,@Now), N'تم الكنس بالكامل', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

-- 1 cancelled
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'صيانة حاويات - الميدان', N'فحص الحاويات', @Z11, @W6, @HealthSup1, 0, 10, 2, 31.9000, 35.2090, 50, DATEADD(DAY,-20,@Now), DATEADD(DAY,-21,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-21,@Now));

PRINT 'Created 29 tasks (6 W1 + 6 W2 + 7 W3 + 10 others)';

-- Backfill LocationDescription for all tasks based on their zone
UPDATE t SET LocationDescription = z.ZoneName + N' - ' +
    CASE t.TaskType
        WHEN 0 THEN N'شوارع ومحاور رئيسية'
        WHEN 1 THEN N'شوارع فرعية وأزقة'
        WHEN 2 THEN N'نقاط تجمع الحاويات'
        WHEN 3 THEN N'مناطق صيانة'
        WHEN 4 THEN N'الساحات والمرافق العامة'
        WHEN 5 THEN N'جولة تفتيشية شاملة'
        ELSE N'موقع العمل'
    END
FROM [Tasks] t
JOIN [Zones] z ON z.ZoneId = t.ZoneId
WHERE t.LocationDescription IS NULL;

-- ========================================================================
-- 7. ISSUES
-- ========================================================================

-- worker1: 3 issues (1 New, 1 InProgress, 1 Resolved)
-- W1-I1: New, reported today - Equipment, Medium
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W1, @Z1, N'حاوية مكسورة الغطاء', N'غطاء الحاوية مكسور ويحتاج استبدال فوري - بجانب مسجد البصبوص', 4, 2, 0, 31.8961, 35.2079, N'بجانب مسجد البصبوص', @Now, @Now, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1);

-- W1-I2: InProgress (3 days ago) - Safety, High
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W1, @Z1, N'عمود إنارة مائل وخطير', N'عمود إنارة مائل بشكل كبير وقد يسقط في أي لحظة - تقاطع شارع 5', 2, 3, 3, 31.8959, 35.2081, N'تقاطع شارع 5 مع الرئيسي', DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1);

-- W1-I3: Resolved (10 days ago) - Cleanliness, Medium
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W1, @Z1, N'تراكم نفايات بجانب حاوية', N'كمية كبيرة من النفايات متراكمة خارج الحاوية - شارع الرئيسي', 3, 2, 2, 31.8962, 35.2083, N'بجانب حاوية شارع الرئيسي', DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now), DATEADD(DAY,-9,@Now), N'تم تنظيف المنطقة بالكامل', @HealthSup1, NULL, NULL, NULL, NULL, 1, 2);

-- worker2: 2 issues (1 New critical, 1 Forwarded)
-- W2-I1: New, reported 2 hours ago - Safety, Critical
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W2, @Z1, N'سلك كهرباء مكشوف', N'سلك كهرباء مكشوف ومتدلي يشكل خطراً كبيراً - مدخل البصبوص الشمالي', 2, 4, 0, 31.8968, 35.2088, N'مدخل البصبوص الشمالي', DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now), NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1);

-- W2-I2: Forwarded to Works dept (2 days ago) - Infrastructure, Critical
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W2, @Z1, N'انهيار جزئي في رصيف', N'انهيار في الرصيف بعد الأمطار الأخيرة يحتاج تدخل فوري', 1, 4, 1, 31.8964, 35.2087, N'الرصيف الغربي لشارع 5', DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now), NULL, NULL, NULL, @WorksDeptId, DATEADD(DAY,-2,@Now), N'تم تحويل للأشغال العامة - يحتاج معدات ثقيلة', @HealthSup1, 1, 1);

-- worker3: 3 issues (1 New, 1 Resolved, 1 Closed)
-- W3-I1: New, reported today - Cleanliness, Medium
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W3, @Z2, N'أوساخ في ساحة عامة', N'تراكم أوساخ وأوراق أشجار في ساحة راس الطاحونة', 3, 2, 0, 31.9069, 35.2121, N'ساحة راس الطاحونة', @Now, @Now, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 1, 1);

-- W3-I2: Resolved (14 days ago) - Infrastructure, High
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W3, @Z2, N'تسرب مياه من ماسورة', N'تسرب مياه ظاهر على سطح الشارع من ماسورة مكسورة', 1, 3, 2, 31.9074, 35.2123, N'شارع راس الطاحونة الفرعي', DATEADD(DAY,-14,@Now), DATEADD(DAY,-14,@Now), DATEADD(DAY,-12,@Now), N'تم إصلاح الماسورة وردم الحفرة', @HealthSup2, NULL, NULL, NULL, NULL, 1, 2);

-- W3-I3: Closed (20 days ago) - Other, Low
INSERT INTO [Issues] (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status, Latitude, Longitude, LocationDescription, ReportedAt, EventTime, ResolvedAt, ResolutionNotes, ResolvedByUserId, ForwardedToDepartmentId, ForwardedAt, ForwardingNotes, ForwardedByUserId, IsSynced, SyncVersion)
VALUES (@MunicipalityId, @W3, @Z2, N'ملاحظة حول لوحة إرشادية', N'لوحة إرشادية قديمة تحتاج تحديث - مدخل منطقة راس الطاحونة', 5, 1, 4, 31.9066, 35.2114, N'مدخل منطقة راس الطاحونة', DATEADD(DAY,-20,@Now), DATEADD(DAY,-20,@Now), DATEADD(DAY,-19,@Now), N'تم الإطلاع - ليست من اختصاص البلدية', @HealthSup2, NULL, NULL, NULL, NULL, 1, 2);

PRINT 'Created 8 issues (3 worker1 + 2 worker2 + 3 worker3)';

-- ========================================================================
-- 8. ATTENDANCE (past 27 days, skip Fri/Sat; all 3 demo workers)
-- ========================================================================
DECLARE @dayOffset INT = 27;
DECLARE @checkIn DATETIME;
DECLARE @checkOut DATETIME;
DECLARE @dow INT;
DECLARE @w1Min INT, @w2Min INT, @w3Min INT;
DECLARE @w2Late INT, @w3Late INT;

WHILE @dayOffset >= 1
BEGIN
    SET @dow = DATEPART(WEEKDAY, DATEADD(DAY, -@dayOffset, @Now));
    -- Skip Friday(6) and Saturday(7)
    IF @dow NOT IN (6, 7)
    BEGIN
        -- Worker1: reliable, 7:00-7:10 (0-10 min offset)
        SET @w1Min = ABS(CHECKSUM(NEWID())) % 11;
        SET @checkIn = DATEADD(MINUTE, @w1Min, DATEADD(HOUR, 7, CAST(CAST(DATEADD(DAY,-@dayOffset,@Now) AS DATE) AS DATETIME)));
        SET @checkOut = DATEADD(HOUR, 8, @checkIn);
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
        VALUES (@MunicipalityId, @W1, @Z1, @checkIn, @checkIn, @checkOut, @checkOut,
            31.896 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.208 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, CAST(5 + ABS(CHECKSUM(NEWID())) % 20 AS FLOAT),
            31.896 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.208 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, CAST(5 + ABS(CHECKSUM(NEWID())) % 20 AS FLOAT),
            1, NULL, CAST('08:00:00' AS TIME), 2, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime');

        -- Worker2: sometimes late, 7:00-7:35 (0-35 min offset)
        SET @w2Min = ABS(CHECKSUM(NEWID())) % 36;
        SET @w2Late = CASE WHEN @w2Min > 15 THEN @w2Min - 15 ELSE 0 END;
        SET @checkIn = DATEADD(MINUTE, @w2Min, DATEADD(HOUR, 7, CAST(CAST(DATEADD(DAY,-@dayOffset,@Now) AS DATE) AS DATETIME)));
        SET @checkOut = DATEADD(HOUR, 8, @checkIn);
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
        VALUES (@MunicipalityId, @W2, @Z1, @checkIn, @checkIn, @checkOut, @checkOut,
            31.896 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.208 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, CAST(5 + ABS(CHECKSUM(NEWID())) % 20 AS FLOAT),
            31.896 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.208 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, CAST(5 + ABS(CHECKSUM(NEWID())) % 20 AS FLOAT),
            1, NULL, CAST('08:00:00' AS TIME), 2, 0, NULL, 'AutoApproved', 1, 1, @w2Late, 0, 0,
            CASE WHEN @w2Late > 0 THEN 'Late' ELSE 'OnTime' END);

        -- Worker3: reliable, occasional late, 7:00-7:20 (0-20 min offset)
        SET @w3Min = ABS(CHECKSUM(NEWID())) % 21;
        SET @w3Late = CASE WHEN @w3Min > 15 THEN @w3Min - 15 ELSE 0 END;
        SET @checkIn = DATEADD(MINUTE, @w3Min, DATEADD(HOUR, 7, CAST(CAST(DATEADD(DAY,-@dayOffset,@Now) AS DATE) AS DATETIME)));
        SET @checkOut = DATEADD(HOUR, 8, @checkIn);
        INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
        VALUES (@MunicipalityId, @W3, @Z2, @checkIn, @checkIn, @checkOut, @checkOut,
            31.907 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.212 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, CAST(5 + ABS(CHECKSUM(NEWID())) % 20 AS FLOAT),
            31.907 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, 35.212 + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001, CAST(5 + ABS(CHECKSUM(NEWID())) % 20 AS FLOAT),
            1, NULL, CAST('08:00:00' AS TIME), 2, 0, NULL, 'AutoApproved', 1, 1, @w3Late, 0, 0,
            CASE WHEN @w3Late > 0 THEN 'Late' ELSE 'OnTime' END);
    END;
    SET @dayOffset = @dayOffset - 1;
END;

-- Today: all 3 checked in (Status=1, no checkout)
-- worker1: checked in at 7:05, zone Z1
INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
VALUES (@MunicipalityId, @W1, @Z1,
    DATEADD(MINUTE,5,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    DATEADD(MINUTE,5,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    NULL, NULL, 31.8961, 35.2081, 8.5, NULL, NULL, NULL,
    1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime');

-- worker2: checked in at 7:22 (Late 7 min), zone Z1
INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
VALUES (@MunicipalityId, @W2, @Z1,
    DATEADD(MINUTE,22,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    DATEADD(MINUTE,22,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    NULL, NULL, 31.8962, 35.2082, 12.0, NULL, NULL, NULL,
    1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 7, 0, 0, 'Late');

-- worker3: checked in at 7:08, zone Z2
INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
VALUES (@MunicipalityId, @W3, @Z2,
    DATEADD(MINUTE,8,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    DATEADD(MINUTE,8,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    NULL, NULL, 31.9072, 35.2118, 9.0, NULL, NULL, NULL,
    1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1, 0, 0, 0, 'OnTime');

PRINT 'Created attendance records (~27 work days x 3 workers + 3 active today)';

-- ========================================================================
-- 9. NOTIFICATIONS
-- ========================================================================
INSERT INTO [Notifications] (MunicipalityId, UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, ReadAt)
VALUES
-- worker1: 4 notifications
(@MunicipalityId, @W1, N'مهمة جديدة', N'تم تعيين مهمة: جمع نفايات صباحي - البصبوص', 1, 1, 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now)),
(@MunicipalityId, @W1, N'مهمة جديدة', N'تم تعيين مهمة: كنس شوارع فرعية - البصبوص', 1, 1, 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now)),
(@MunicipalityId, @W1, N'مهمة جديدة', N'تم تعيين مهمة: جمع نفايات - جامعة بيرزيت', 1, 0, 1, @Now, @Now, NULL),
(@MunicipalityId, @W1, N'تذكير بمهمة', N'لديك 3 مهام مستحقة اليوم - يرجى البدء', 2, 0, 1, @Now, @Now, NULL),
-- worker2: 4 notifications
(@MunicipalityId, @W2, N'مهمة جديدة', N'تم تعيين مهمة: جمع نفايات - البصبوص شمال', 1, 1, 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now)),
(@MunicipalityId, @W2, N'مهمة جديدة', N'تم تعيين مهمة: تنظيف محيط المدرسة', 1, 1, 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now), DATEADD(DAY,-1,@Now)),
(@MunicipalityId, @W2, N'مهمة جديدة', N'تم تعيين مهمة: تنظيف ساحة جامعة بيرزيت', 1, 0, 1, @Now, @Now, NULL),
(@MunicipalityId, @W2, N'تذكير بمهمة', N'لديك 3 مهام مستحقة اليوم - يرجى البدء', 2, 0, 1, @Now, @Now, NULL),
-- worker3: 5 notifications
(@MunicipalityId, @W3, N'مهمة جديدة', N'تم تعيين مهمة: جمع نفايات - راس الطاحونة', 1, 1, 1, DATEADD(DAY,-21,@Now), DATEADD(DAY,-21,@Now), DATEADD(DAY,-21,@Now)),
(@MunicipalityId, @W3, N'مهمة مقبولة', N'تمت الموافقة على مهمة كنس شوارع راس الطاحونة', 3, 1, 1, DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now), DATEADD(DAY,-10,@Now)),
(@MunicipalityId, @W3, N'مهمة مرفوضة', N'تم رفض مهمة جمع نفايات مسائي - الصور غير واضحة', 3, 1, 1, DATEADD(DAY,-5,@Now), DATEADD(DAY,-5,@Now), DATEADD(DAY,-5,@Now)),
(@MunicipalityId, @W3, N'مهمة قيد المراجعة', N'مهمتك صيانة حاويات راس الطاحونة قيد المراجعة', 3, 0, 1, DATEADD(HOUR,-1,@Now), DATEADD(HOUR,-1,@Now), NULL),
(@MunicipalityId, @W3, N'تذكير بمهمة', N'لديك مهمة تفتيشية مستحقة اليوم', 2, 0, 1, @Now, @Now, NULL),
-- super1: 4 notifications
(@MunicipalityId, @HealthSup1, N'مهمة قيد المراجعة', N'العامل محمد عبد الله قدّم مهمة صيانة حاويات للمراجعة', 3, 1, 1, DATEADD(HOUR,-5,@Now), DATEADD(HOUR,-5,@Now), DATEADD(HOUR,-4,@Now)),
(@MunicipalityId, @HealthSup1, N'بلاغ جديد', N'تم الإبلاغ عن عمود إنارة مائل في منطقة البصبوص', 11, 1, 1, DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now), DATEADD(DAY,-3,@Now)),
(@MunicipalityId, @HealthSup1, N'بلاغ حرج', N'بلاغ حرج: سلك كهرباء مكشوف في منطقة البصبوص', 11, 0, 1, DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now), NULL),
(@MunicipalityId, @HealthSup1, N'مهام معلقة', N'لديك مهمة قيد المراجعة تحتاج قراراً', 3, 0, 1, @Now, @Now, NULL),
-- super2: 4 notifications
(@MunicipalityId, @HealthSup2, N'مهمة قيد المراجعة', N'العامل أنس درويش قدّم مهمة صيانة حاويات راس الطاحونة للمراجعة', 3, 0, 1, DATEADD(HOUR,-1,@Now), DATEADD(HOUR,-1,@Now), NULL),
(@MunicipalityId, @HealthSup2, N'بلاغ جديد', N'تم الإبلاغ عن تراكم أوساخ في ساحة راس الطاحونة', 11, 0, 1, @Now, @Now, NULL),
(@MunicipalityId, @HealthSup2, N'طعن جديد', N'العامل أنس درويش قدّم طعناً على رفض مهمته', 13, 1, 1, DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now)),
(@MunicipalityId, @HealthSup2, N'تسجيل دخول', N'تم تسجيل دخولك إلى النظام بنجاح', 5, 1, 1, DATEADD(HOUR,-3,@Now), DATEADD(HOUR,-3,@Now), DATEADD(HOUR,-3,@Now)),
-- admin: 3 notifications
(@MunicipalityId, @AdminId, N'تسجيل دخول', N'تم تسجيل دخولك إلى النظام بنجاح', 5, 1, 1, DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now)),
(@MunicipalityId, @AdminId, N'تنبيه نظام', N'يوجد 2 بلاغ حرج بحاجة لمراجعة فورية', 5, 0, 1, DATEADD(HOUR,-1,@Now), DATEADD(HOUR,-1,@Now), NULL),
(@MunicipalityId, @AdminId, N'إحصائيات يومية', N'تقرير اليوم: 12 مهمة نشطة، 3 بلاغات جديدة، 148 عامل مسجّل', 5, 0, 1, @Now, @Now, NULL);

PRINT 'Created 24 notifications (4 W1 + 4 W2 + 5 W3 + 4 super1 + 4 super2 + 3 admin)';

-- ========================================================================
-- 10. APPEALS
-- ========================================================================
-- Resolve the rejected task for worker3 (W3-T4)
DECLARE @W3RejectedTask INT;
SELECT TOP 1 @W3RejectedTask = TaskId FROM [Tasks]
    WHERE AssignedToUserId = @W3 AND Status = 4 ORDER BY CreatedAt;

-- Appeal 1: worker3 pending appeal on rejected task (submitted 2 days ago)
INSERT INTO [Appeals] (MunicipalityId, AppealType, EntityType, EntityId, UserId, WorkerExplanation, WorkerLatitude, WorkerLongitude, ExpectedLatitude, ExpectedLongitude, DistanceMeters, Status, ReviewedByUserId, ReviewedAt, ReviewNotes, SubmittedAt, CreatedAt, OriginalRejectionReason, IsSynced, SyncVersion)
VALUES (@MunicipalityId, 1, 'Task', @W3RejectedTask, @W3,
    N'الصور كانت واضحة ولكن ربما حدث خطأ في الرفع - أرجو إعادة النظر',
    31.9070, 35.2116, 31.9070, 35.2116, 5,
    1, NULL, NULL, NULL,
    DATEADD(DAY,-2,@Now), DATEADD(DAY,-2,@Now),
    N'الصور غير واضحة - يرجى إعادة التصوير', 1, 1);

-- Appeal 2: worker3 approved appeal from 15 days ago (attendance appeal)
DECLARE @W3AttendanceId INT;
SELECT TOP 1 @W3AttendanceId = AttendanceId FROM [Attendances]
    WHERE UserId = @W3 ORDER BY CheckInEventTime;

INSERT INTO [Appeals] (MunicipalityId, AppealType, EntityType, EntityId, UserId, WorkerExplanation, WorkerLatitude, WorkerLongitude, ExpectedLatitude, ExpectedLongitude, DistanceMeters, Status, ReviewedByUserId, ReviewedAt, ReviewNotes, SubmittedAt, CreatedAt, OriginalRejectionReason, IsSynced, SyncVersion)
VALUES (@MunicipalityId, 2, 'Attendance', ISNULL(@W3AttendanceId, 1), @W3,
    N'لم أتمكن من تسجيل الحضور بسبب مشكلة في الجهاز - كنت في الموقع فعلياً',
    31.9068, 35.2120, 31.9070, 35.2120, 15,
    2, @HealthSup2, DATEADD(DAY,-13,@Now), N'تمت الموافقة - تم التحقق من وجود العامل في الموقع',
    DATEADD(DAY,-15,@Now), DATEADD(DAY,-15,@Now),
    N'غياب بدون عذر', 1, 1);

PRINT 'Created 2 appeals for worker3 (1 pending + 1 approved)';

-- ========================================================================
-- 11. AUDIT LOGS (~40 entries)
-- ========================================================================
INSERT INTO [AuditLogs] (UserId, Username, Action, Details, IpAddress, UserAgent, CreatedAt)
VALUES
-- Admin logins (5 entries)
(@AdminId, 'admin', 'Login', N'تسجيل دخول ناجح', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-28,@Now)),
(@AdminId, 'admin', 'Login', N'تسجيل دخول ناجح', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-21,@Now)),
(@AdminId, 'admin', 'Login', N'تسجيل دخول ناجح', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-14,@Now)),
(@AdminId, 'admin', 'Login', N'تسجيل دخول ناجح', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-7,@Now)),
(@AdminId, 'admin', 'Login', N'تسجيل دخول ناجح', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(HOUR,-2,@Now)),
-- super1: logins + task creates + approvals (8 entries)
(@HealthSup1, 'super1', 'Login', N'تسجيل دخول ناجح', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-25,@Now)),
(@HealthSup1, 'super1', 'Login', N'تسجيل دخول ناجح', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-15,@Now)),
(@HealthSup1, 'super1', 'Login', N'تسجيل دخول ناجح', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-7,@Now)),
(@HealthSup1, 'super1', 'Login', N'تسجيل دخول ناجح', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(HOUR,-3,@Now)),
(@HealthSup1, 'super1', 'TaskCreate', N'إنشاء مهمة جمع نفايات لـ worker1 - البصبوص', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-1,@Now)),
(@HealthSup1, 'super1', 'TaskCreate', N'إنشاء مهمة كنس شوارع لـ worker2 - البصبوص', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-1,@Now)),
(@HealthSup1, 'super1', 'IssueForward', N'تحويل بلاغ انهيار رصيف إلى دائرة الأشغال', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-2,@Now)),
(@HealthSup1, 'super1', 'IssueResolve', N'حل بلاغ تراكم نفايات - منطقة البصبوص', '192.168.1.101', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-9,@Now)),
-- super2: logins + task creates + review actions (8 entries)
(@HealthSup2, 'super2', 'Login', N'تسجيل دخول ناجح', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-22,@Now)),
(@HealthSup2, 'super2', 'Login', N'تسجيل دخول ناجح', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-12,@Now)),
(@HealthSup2, 'super2', 'Login', N'تسجيل دخول ناجح', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-5,@Now)),
(@HealthSup2, 'super2', 'Login', N'تسجيل دخول ناجح', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(HOUR,-3,@Now)),
(@HealthSup2, 'super2', 'TaskCreate', N'إنشاء مهمة جمع نفايات لـ worker3 - راس الطاحونة', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-21,@Now)),
(@HealthSup2, 'super2', 'TaskApprove', N'الموافقة على مهمة جمع نفايات - worker3', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-20,@Now)),
(@HealthSup2, 'super2', 'TaskApprove', N'الموافقة على مهمة كنس شوارع - worker3', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-10,@Now)),
(@HealthSup2, 'super2', 'TaskReject', N'رفض مهمة جمع نفايات مسائي - worker3: الصور غير واضحة', '192.168.1.102', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-5,@Now)),
-- worker1: logins + check-ins + issue reports (6 entries)
(@W1, 'worker1', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-20,@Now)),
(@W1, 'worker1', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-10,@Now)),
(@W1, 'worker1', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W1, 'worker1', 'CheckIn', N'تسجيل حضور - منطقة البصبوص', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W1, 'worker1', 'IssueReport', N'إبلاغ عن حاوية مكسورة الغطاء - بجانب مسجد البصبوص', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W1, 'worker1', 'IssueReport', N'إبلاغ عن عمود إنارة مائل - تقاطع شارع 5', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-3,@Now)),
-- worker2: logins + check-ins (4 entries)
(@W2, 'worker2', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-18,@Now)),
(@W2, 'worker2', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W2, 'worker2', 'CheckIn', N'تسجيل حضور - منطقة البصبوص', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W2, 'worker2', 'IssueReport', N'إبلاغ عن سلك كهرباء مكشوف - مدخل البصبوص', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', DATEADD(HOUR,-2,@Now)),
-- worker3: logins + check-ins + issues + appeals (6 entries)
(@W3, 'worker3', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.52', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-20,@Now)),
(@W3, 'worker3', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.52', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-10,@Now)),
(@W3, 'worker3', 'Login', N'تسجيل دخول من التطبيق', '10.0.0.52', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W3, 'worker3', 'CheckIn', N'تسجيل حضور - منطقة راس الطاحونة', '10.0.0.52', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W3, 'worker3', 'IssueReport', N'إبلاغ عن تراكم أوساخ في ساحة راس الطاحونة', '10.0.0.52', 'FollowUp-Mobile/1.0 (Android)', @Now),
(@W3, 'worker3', 'AppealSubmit', N'تقديم طعن على رفض مهمة جمع نفايات مسائي', '10.0.0.52', 'FollowUp-Mobile/1.0 (Android)', DATEADD(DAY,-2,@Now)),
-- 2 failed login attempts
(NULL, 'hacker123', 'LoginFailed', N'محاولة تسجيل دخول فاشلة - مستخدم غير موجود', '185.220.101.42', 'curl/7.68.0', DATEADD(DAY,-5,@Now)),
(NULL, 'test', 'LoginFailed', N'محاولة تسجيل دخول فاشلة - كلمة مرور خاطئة', '185.220.101.42', 'Python-urllib/3.9', DATEADD(DAY,-3,@Now)),
-- 1 password reset
(@W1, 'worker1', 'PasswordReset', N'إعادة تعيين كلمة المرور بواسطة المشرف', '192.168.1.100', 'Mozilla/5.0 (Windows NT 10.0)', DATEADD(DAY,-10,@Now));

PRINT 'Created ~40 audit log entries';

-- ========================================================================
-- 12. LOCATION HISTORIES
-- ========================================================================
INSERT INTO [LocationHistories] (UserId, Latitude, Longitude, Speed, Accuracy, Heading, Timestamp, IsSync)
VALUES
-- worker1: 15 GPS points (past 3 days + today, Z1 area)
(@W1, 31.8955, 35.2075, 1.2, 8.5, 45.0,  DATEADD(HOUR,-72,@Now), 1),
(@W1, 31.8958, 35.2078, 2.1, 6.0, 90.0,  DATEADD(HOUR,-71,@Now), 1),
(@W1, 31.8962, 35.2082, 1.8, 7.2, 120.0, DATEADD(HOUR,-70,@Now), 1),
(@W1, 31.8965, 35.2085, 0.5, 5.5, 180.0, DATEADD(HOUR,-69,@Now), 1),
(@W1, 31.8960, 35.2080, 1.5, 9.0, 270.0, DATEADD(HOUR,-68,@Now), 1),
(@W1, 31.8957, 35.2077, 1.0, 7.0, 30.0,  DATEADD(HOUR,-48,@Now), 1),
(@W1, 31.8961, 35.2081, 2.3, 5.8, 60.0,  DATEADD(HOUR,-47,@Now), 1),
(@W1, 31.8964, 35.2084, 1.6, 8.1, 150.0, DATEADD(HOUR,-46,@Now), 1),
(@W1, 31.8968, 35.2088, 0.8, 6.5, 200.0, DATEADD(HOUR,-45,@Now), 1),
(@W1, 31.8963, 35.2083, 1.9, 7.8, 310.0, DATEADD(HOUR,-44,@Now), 1),
(@W1, 31.8956, 35.2076, 0.9, 8.0, 15.0,  DATEADD(HOUR,-4,@Now),  1),
(@W1, 31.8960, 35.2080, 2.0, 5.5, 75.0,  DATEADD(HOUR,-3,@Now),  1),
(@W1, 31.8963, 35.2083, 1.4, 6.8, 135.0, DATEADD(HOUR,-2,@Now),  1),
(@W1, 31.8967, 35.2087, 1.1, 7.5, 225.0, DATEADD(HOUR,-1,@Now),  1),
(@W1, 31.8961, 35.2081, 0.6, 9.2, 350.0, @Now,                   1),
-- worker2: 12 GPS points (past 2 days + today, Z1 area)
(@W2, 31.8953, 35.2077, 1.3, 7.0, 40.0,  DATEADD(HOUR,-48,@Now), 1),
(@W2, 31.8956, 35.2080, 2.0, 5.5, 85.0,  DATEADD(HOUR,-47,@Now), 1),
(@W2, 31.8960, 35.2084, 1.7, 8.0, 130.0, DATEADD(HOUR,-46,@Now), 1),
(@W2, 31.8963, 35.2087, 0.9, 6.2, 190.0, DATEADD(HOUR,-45,@Now), 1),
(@W2, 31.8958, 35.2082, 1.5, 7.5, 280.0, DATEADD(HOUR,-44,@Now), 1),
(@W2, 31.8955, 35.2077, 1.1, 6.8, 25.0,  DATEADD(HOUR,-4,@Now),  1),
(@W2, 31.8958, 35.2080, 1.8, 5.0, 70.0,  DATEADD(HOUR,-3,@Now),  1),
(@W2, 31.8962, 35.2083, 2.2, 7.3, 140.0, DATEADD(HOUR,-2,@Now),  1),
(@W2, 31.8965, 35.2086, 0.7, 8.5, 210.0, DATEADD(HOUR,-1,@Now),  1),
(@W2, 31.8960, 35.2082, 1.0, 6.0, 330.0, @Now,                   1),
(@W2, 31.8957, 35.2079, 1.4, 7.2, 55.0,  DATEADD(MINUTE,-30,@Now),1),
(@W2, 31.8961, 35.2083, 0.8, 8.1, 95.0,  DATEADD(MINUTE,-15,@Now),1),
-- worker3: 12 GPS points (past 2 days + today, Z2 area)
(@W3, 31.9065, 35.2115, 1.3, 7.0, 40.0,  DATEADD(HOUR,-48,@Now), 1),
(@W3, 31.9068, 35.2118, 2.0, 5.5, 85.0,  DATEADD(HOUR,-47,@Now), 1),
(@W3, 31.9072, 35.2122, 1.7, 8.0, 130.0, DATEADD(HOUR,-46,@Now), 1),
(@W3, 31.9075, 35.2125, 0.9, 6.2, 190.0, DATEADD(HOUR,-45,@Now), 1),
(@W3, 31.9070, 35.2120, 1.5, 7.5, 280.0, DATEADD(HOUR,-44,@Now), 1),
(@W3, 31.9066, 35.2116, 1.1, 6.8, 25.0,  DATEADD(HOUR,-4,@Now),  1),
(@W3, 31.9069, 35.2119, 1.8, 5.0, 70.0,  DATEADD(HOUR,-3,@Now),  1),
(@W3, 31.9073, 35.2123, 2.2, 7.3, 140.0, DATEADD(HOUR,-2,@Now),  1),
(@W3, 31.9076, 35.2126, 0.7, 8.5, 210.0, DATEADD(HOUR,-1,@Now),  1),
(@W3, 31.9071, 35.2121, 1.0, 6.0, 330.0, @Now,                   1),
(@W3, 31.9068, 35.2118, 1.4, 7.2, 55.0,  DATEADD(MINUTE,-30,@Now),1),
(@W3, 31.9072, 35.2122, 0.8, 8.1, 95.0,  DATEADD(MINUTE,-15,@Now),1);

PRINT 'Created 39 location history points (15 W1 + 12 W2 + 12 W3)';

-- ========================================================================
-- 13. PHOTOS
-- ========================================================================
-- Resolve task IDs for worker3 completed tasks
DECLARE @W3T1 INT, @W3T2 INT;
SELECT TOP 1 @W3T1 = TaskId FROM [Tasks] WHERE AssignedToUserId = @W3 AND Status = 3 ORDER BY CreatedAt ASC;
SELECT @W3T2 = TaskId FROM [Tasks] WHERE AssignedToUserId = @W3 AND Status = 3 ORDER BY CreatedAt DESC OFFSET 0 ROWS FETCH NEXT 1 ROWS ONLY;

-- Resolve issue IDs
DECLARE @W1I1 INT, @W1I2 INT, @W2I1 INT, @W3I1 INT;
SELECT TOP 1 @W1I1 = IssueId FROM [Issues] WHERE ReportedByUserId = @W1 AND Status = 0 ORDER BY ReportedAt DESC;
SELECT TOP 1 @W1I2 = IssueId FROM [Issues] WHERE ReportedByUserId = @W1 AND Status = 3 ORDER BY ReportedAt DESC;
SELECT TOP 1 @W2I1 = IssueId FROM [Issues] WHERE ReportedByUserId = @W2 AND Status = 0 ORDER BY ReportedAt DESC;
SELECT TOP 1 @W3I1 = IssueId FROM [Issues] WHERE ReportedByUserId = @W3 AND Status = 0 ORDER BY ReportedAt DESC;

-- Task photos: 2 per completed task (before/after) for worker3
INSERT INTO [Photos] (PhotoUrl, EntityType, EntityId, TaskId, IssueId, OrderIndex, FileSizeBytes, UploadedAt, UploadedByUserId, CreatedAt)
VALUES
('/uploads/tasks/w3_t1_before.jpg', 'Task', @W3T1, @W3T1, NULL, 0, 245000, DATEADD(DAY,-20,@Now), @W3, DATEADD(DAY,-20,@Now)),
('/uploads/tasks/w3_t1_after.jpg',  'Task', @W3T1, @W3T1, NULL, 1, 312000, DATEADD(DAY,-20,@Now), @W3, DATEADD(DAY,-20,@Now)),
('/uploads/tasks/w3_t2_before.jpg', 'Task', @W3T2, @W3T2, NULL, 0, 198000, DATEADD(DAY,-10,@Now), @W3, DATEADD(DAY,-10,@Now)),
('/uploads/tasks/w3_t2_after.jpg',  'Task', @W3T2, @W3T2, NULL, 1, 275000, DATEADD(DAY,-10,@Now), @W3, DATEADD(DAY,-10,@Now));

-- Issue photos: 1 per issue
INSERT INTO [Photos] (PhotoUrl, EntityType, EntityId, TaskId, IssueId, OrderIndex, FileSizeBytes, UploadedAt, UploadedByUserId, CreatedAt)
VALUES
('/uploads/issues/w1_i1_container.jpg', 'Issue', @W1I1, NULL, @W1I1, 0, 280000, @Now,                   @W1, @Now),
('/uploads/issues/w1_i2_pole.jpg',      'Issue', @W1I2, NULL, @W1I2, 0, 340000, DATEADD(DAY,-3,@Now),   @W1, DATEADD(DAY,-3,@Now)),
('/uploads/issues/w2_i1_wire.jpg',      'Issue', @W2I1, NULL, @W2I1, 0, 390000, DATEADD(HOUR,-2,@Now),  @W2, DATEADD(HOUR,-2,@Now)),
('/uploads/issues/w3_i1_dirt.jpg',      'Issue', @W3I1, NULL, @W3I1, 0, 215000, @Now,                   @W3, @Now);

PRINT 'Created 8 photos (4 task evidence + 4 issue reports)';

-- ========================================================================
-- 14. TASK TEMPLATES (9 templates + 1 BZU = 10 total)
-- ========================================================================
INSERT INTO [TaskTemplates] (Title, Description, MunicipalityId, ZoneId, Frequency, Time, IsActive, LastGeneratedAt, CreatedAt, Priority, TaskType, RequiresPhotoProof, EstimatedDurationMinutes, LocationDescription, DefaultAssignedToUserId, DefaultTeamId, IsTeamTask)
VALUES
(N'جمع نفايات يومي - البصبوص',       N'جولة جمع نفايات صباحية يومية في منطقة البصبوص',            @MunicipalityId, @Z1,   N'Daily',   '06:00:00', 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-30,@Now), 1, 0, 1, 120, N'شوارع حي البصبوص الرئيسية والفرعية',  @W1, NULL, 0),
(N'جمع نفايات يومي - الميدان',        N'جولة جمع نفايات صباحية يومية في منطقة الميدان',             @MunicipalityId, @Z11,  N'Daily',   '06:00:00', 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-30,@Now), 1, 0, 1, 120, N'شوارع حي الميدان ومحيط الساحة',       NULL, NULL, 0),
(N'تنظيف شارع البلدية الرئيسي',       N'كنس وتنظيف يومي للشارع الرئيسي',                            @MunicipalityId, @Z5,   N'Daily',   '06:30:00', 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-28,@Now), 1, 1, 1, 90,  N'الشارع الرئيسي أمام مبنى البلدية',    NULL, NULL, 0),
(N'جمع نفايات يومي - حديقة حرب',      N'جولة جمع وتنظيف محيط الحديقة',                              @MunicipalityId, @Z3,   N'Daily',   '07:00:00', 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-25,@Now), 1, 4, 1, 90,  N'حديقة حرب العامة والمنطقة المحيطة',  NULL, NULL, 0),
(N'تعقيم حاويات - راس الطاحونة',      N'غسل وتعقيم جميع الحاويات في المنطقة',                       @MunicipalityId, @Z2,   N'Weekly',  '07:00:00', 1, DATEADD(DAY,-5,@Now), DATEADD(DAY,-30,@Now), 1, 2, 1, 180, N'جميع حاويات منطقة راس الطاحونة',     @W3, NULL, 0),
(N'تعقيم حاويات - البص',              N'غسل وتعقيم الحاويات',                                        @MunicipalityId, @Z6,   N'Weekly',  '07:00:00', 1, DATEADD(DAY,-4,@Now), DATEADD(DAY,-28,@Now), 1, 2, 1, 150, N'حاويات حي البص',                      NULL, NULL, 0),
(N'ري المسطحات الخضراء - حديقة حرب',  N'ري الأشجار والمسطحات الخضراء في الحديقة',                    @MunicipalityId, @Z3,   N'Weekly',  '06:00:00', 1, DATEADD(DAY,-3,@Now), DATEADD(DAY,-27,@Now), 0, 4, 0, 120, N'المسطحات الخضراء في حديقة حرب',       NULL, @AgriTeam1, 1),
(N'فحص شبكة الإنارة - المنطقة الوسطى',N'فحص شامل لجميع أعمدة الإنارة وتوثيق الأعطال',               @MunicipalityId, @Z13,  N'Monthly', '08:00:00', 1, DATEADD(DAY,-15,@Now),DATEADD(DAY,-30,@Now), 2, 5, 1, 240, N'جميع أعمدة الإنارة في منطقة المركز', NULL, @WorksTeam1, 1),
(N'تنظيف ساحة المنارة',               N'ألغيت بسبب أعمال بناء في المنطقة',                           @MunicipalityId, @Z17,  N'Daily',   '06:00:00', 0, DATEADD(DAY,-12,@Now),DATEADD(DAY,-30,@Now), 1, 4, 1, 60,  N'ساحة المنارة العامة',                 NULL, NULL, 0),
(N'جمع نفايات يومي - جامعة بيرزيت',   N'جولة جمع نفايات صباحية يومية في محيط جامعة بيرزيت',         @MunicipalityId, @ZBZU, N'Daily',   '06:30:00', 1, DATEADD(DAY,-1,@Now), DATEADD(DAY,-10,@Now), 1, 0, 1, 90,  N'مدخل الجامعة والمباني الرئيسية',     @W1, NULL, 0);

PRINT 'Created 10 task templates (4 daily, 3 weekly, 1 monthly, 1 inactive, 1 BZU)';

-- ========================================================================
-- 15. DEMO POLISH: ALL users demo-ready
-- ========================================================================

-- 15.1 LastLoginAt: ALL active users get recent login times
-- Demo accounts: 3 min ago
UPDATE [Users] SET LastLoginAt = DATEADD(MINUTE, -3, @Now)
    WHERE Username IN ('worker1', 'worker2', 'worker3', 'super1', 'super2', 'admin');

-- All other active workers: random login within last 2 hours (appear online/recent)
DECLARE @loginIdx INT = 1;
WHILE @loginIdx <= 148
BEGIN
    UPDATE [Users]
    SET LastLoginAt = DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % 120), @Now),
        LastBatteryLevel = 40 + (ABS(CHECKSUM(NEWID())) % 55), -- 40-95%
        IsLowBattery = CASE WHEN (ABS(CHECKSUM(NEWID())) % 10) = 0 THEN 1 ELSE 0 END -- 10% low battery
    WHERE Username = 'worker' + CAST(@loginIdx AS VARCHAR(10))
      AND Status = 0 -- only active
      AND LastLoginAt IS NULL; -- don't overwrite demo accounts
    SET @loginIdx = @loginIdx + 1;
END;

-- All supervisors recent login
UPDATE [Users] SET LastLoginAt = DATEADD(MINUTE, -(ABS(CHECKSUM(NEWID())) % 60), @Now)
    WHERE Role = 1 AND Status = 0 AND LastLoginAt IS NULL;

PRINT 'All active users have recent LastLoginAt + battery levels';

-- 15.2 TODAY attendance for ALL active workers (bulk check-in)
DECLARE @attIdx INT = 1;
DECLARE @attUserId INT;
DECLARE @attZoneId INT;
DECLARE @attMinOffset INT;
DECLARE @attCheckIn DATETIME;

WHILE @attIdx <= 148
BEGIN
    SELECT @attUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@attIdx AS VARCHAR(10)) AND Status = 0;

    IF @attUserId IS NOT NULL
    BEGIN
        -- skip demo workers (already have today's attendance)
        IF @attIdx NOT IN (1, 2, 3)
        BEGIN
            -- Get their assigned zone
            SELECT TOP 1 @attZoneId = uz.ZoneId FROM [UserZones] uz WHERE uz.UserId = @attUserId AND uz.IsActive = 1;
            IF @attZoneId IS NULL SET @attZoneId = @Z1; -- fallback

            -- Random check-in 7:00-7:25
            SET @attMinOffset = ABS(CHECKSUM(NEWID())) % 26;
            SET @attCheckIn = DATEADD(MINUTE, @attMinOffset, DATEADD(HOUR, 7, CAST(CAST(@Now AS DATE) AS DATETIME)));

            -- Get zone center coordinates for realistic GPS
            DECLARE @attLat FLOAT, @attLng FLOAT;
            SELECT @attLat = CenterLatitude, @attLng = CenterLongitude FROM [Zones] WHERE ZoneId = @attZoneId;
            IF @attLat IS NULL BEGIN SET @attLat = 31.900; SET @attLng = 35.212; END;

            INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime, CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters, CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage, WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion, LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
            VALUES (@MunicipalityId, @attUserId, @attZoneId, @attCheckIn, @attCheckIn, NULL, NULL,
                @attLat + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                @attLng + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                CAST(5 + ABS(CHECKSUM(NEWID())) % 15 AS FLOAT),
                NULL, NULL, NULL, 1, NULL, NULL, 1, 0, NULL, 'AutoApproved', 1, 1,
                CASE WHEN @attMinOffset > 15 THEN @attMinOffset - 15 ELSE 0 END, 0, 0,
                CASE WHEN @attMinOffset > 15 THEN 'Late' ELSE 'OnTime' END);
        END;
    END;
    SET @attIdx = @attIdx + 1;
END;

PRINT 'Today attendance check-in for all active workers';

-- 15.25 HISTORICAL attendance for ALL workers (past 7 work days)
-- This makes the monthly reports chart show realistic data
DECLARE @histDay INT = 7;
DECLARE @histDate DATE;
DECLARE @histDow INT;
DECLARE @histWIdx INT;
DECLARE @histUserId INT;
DECLARE @histZoneId INT;
DECLARE @histMinOff INT;
DECLARE @histIn DATETIME;
DECLARE @histOut DATETIME;
DECLARE @histLat FLOAT;
DECLARE @histLng FLOAT;
DECLARE @histAbsentChance INT;

WHILE @histDay >= 1
BEGIN
    SET @histDate = CAST(DATEADD(DAY, -@histDay, @Now) AS DATE);
    SET @histDow = DATEPART(WEEKDAY, @histDate);

    -- Skip Friday(6) and Saturday(7)
    IF @histDow NOT IN (6, 7)
    BEGIN
        SET @histWIdx = 4; -- start from worker4 (worker1-3 already have history)
        WHILE @histWIdx <= 148
        BEGIN
            SELECT @histUserId = UserId FROM [Users]
                WHERE Username = 'worker' + CAST(@histWIdx AS VARCHAR(10)) AND Status = 0;

            IF @histUserId IS NOT NULL
            BEGIN
                -- 90% attendance rate (random 10% absent)
                SET @histAbsentChance = ABS(CHECKSUM(NEWID())) % 100;
                IF @histAbsentChance >= 10  -- 90% show up
                BEGIN
                    SELECT TOP 1 @histZoneId = uz.ZoneId FROM [UserZones] uz
                        WHERE uz.UserId = @histUserId AND uz.IsActive = 1;
                    IF @histZoneId IS NULL SET @histZoneId = @Z1;

                    SELECT @histLat = CenterLatitude, @histLng = CenterLongitude
                        FROM [Zones] WHERE ZoneId = @histZoneId;
                    IF @histLat IS NULL BEGIN SET @histLat = 31.900; SET @histLng = 35.212; END;

                    -- Check-in 7:00-7:30, checkout after 8 hours
                    SET @histMinOff = ABS(CHECKSUM(NEWID())) % 31;
                    SET @histIn = DATEADD(MINUTE, @histMinOff, DATEADD(HOUR, 7, CAST(@histDate AS DATETIME)));
                    SET @histOut = DATEADD(HOUR, 8, @histIn);

                    INSERT INTO [Attendances] (MunicipalityId, UserId, ZoneId, CheckInEventTime, CheckInSyncTime,
                        CheckOutEventTime, CheckOutSyncTime, CheckInLatitude, CheckInLongitude, CheckInAccuracyMeters,
                        CheckOutLatitude, CheckOutLongitude, CheckOutAccuracyMeters, IsValidated, ValidationMessage,
                        WorkDuration, Status, IsManual, ManualReason, ApprovalStatus, IsSynced, SyncVersion,
                        LateMinutes, EarlyLeaveMinutes, OvertimeMinutes, AttendanceType)
                    VALUES (@MunicipalityId, @histUserId, @histZoneId, @histIn, @histIn, @histOut, @histOut,
                        @histLat + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                        @histLng + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                        CAST(5 + ABS(CHECKSUM(NEWID())) % 15 AS FLOAT),
                        @histLat + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                        @histLng + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                        CAST(5 + ABS(CHECKSUM(NEWID())) % 15 AS FLOAT),
                        1, NULL, CAST('08:00:00' AS TIME), 2, 0, NULL, 'AutoApproved', 1, 1,
                        CASE WHEN @histMinOff > 15 THEN @histMinOff - 15 ELSE 0 END, 0, 0,
                        CASE WHEN @histMinOff > 15 THEN 'Late' ELSE 'OnTime' END);
                END;
            END;
            SET @histWIdx = @histWIdx + 1;
        END;
    END;
    SET @histDay = @histDay - 1;
END;

PRINT 'Historical attendance for all workers (past 7 work days, ~90% rate)';

-- 15.3 Zone assignments for Works and Agriculture workers
DECLARE @wzIdx INT = 101;
DECLARE @wzUserId INT;
DECLARE @wzZoneIdx INT;

-- Works workers (101-130): assign to zones Z3-Z12
WHILE @wzIdx <= 130
BEGIN
    SELECT @wzUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@wzIdx AS VARCHAR(10));
    IF @wzUserId IS NOT NULL
    BEGIN
        SET @wzZoneIdx = ((@wzIdx - 101) / 3) + 3; -- zones 3-12
        IF @wzZoneIdx > 12 SET @wzZoneIdx = 3 + ((@wzIdx - 101) % 10);
        DECLARE @wzZone INT;
        SELECT @wzZone = ZoneId FROM @ZoneIds WHERE idx = @wzZoneIdx;
        IF @wzZone IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [UserZones] WHERE UserId = @wzUserId AND ZoneId = @wzZone)
                INSERT INTO [UserZones] (UserId, ZoneId, AssignedAt, AssignedByUserId, IsActive) VALUES (@wzUserId, @wzZone, @Now, @WorksSup1, 1);
        END;
    END;
    SET @wzIdx = @wzIdx + 1;
END;

-- Agriculture workers (131-148): assign to zones Z13-Z20
SET @wzIdx = 131;
WHILE @wzIdx <= 148
BEGIN
    SELECT @wzUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@wzIdx AS VARCHAR(10));
    IF @wzUserId IS NOT NULL
    BEGIN
        SET @wzZoneIdx = 13 + ((@wzIdx - 131) % 8); -- zones 13-20
        SELECT @wzZone = ZoneId FROM @ZoneIds WHERE idx = @wzZoneIdx;
        IF @wzZone IS NOT NULL
        BEGIN
            IF NOT EXISTS (SELECT 1 FROM [UserZones] WHERE UserId = @wzUserId AND ZoneId = @wzZone)
                INSERT INTO [UserZones] (UserId, ZoneId, AssignedAt, AssignedByUserId, IsActive) VALUES (@wzUserId, @wzZone, @Now, @AgriSup1, 1);
        END;
    END;
    SET @wzIdx = @wzIdx + 1;
END;

PRINT 'Zone assignments for Works and Agriculture workers';

-- 15.4 Tasks for ALL workers: each active worker gets 2-3 tasks
DECLARE @taskIdx INT = 4; -- start from worker4 (W1-W3 already have tasks, W4-W10 have some)
DECLARE @taskUserId INT;
DECLARE @taskZone INT;
DECLARE @taskSupervisor INT;
DECLARE @taskLat FLOAT;
DECLARE @taskLng FLOAT;
DECLARE @taskTitles TABLE (idx INT IDENTITY(1,1), Title NVARCHAR(200), Desc_ NVARCHAR(500), TaskType INT);
INSERT INTO @taskTitles (Title, Desc_, TaskType) VALUES
(N'جمع نفايات يومي', N'جولة جمع نفايات صباحية شاملة في المنطقة', 0),
(N'كنس شوارع', N'كنس وتنظيف الشوارع الرئيسية والفرعية', 1),
(N'صيانة حاويات', N'فحص وتنظيف الحاويات في المنطقة', 2),
(N'تنظيف ساحة عامة', N'تنظيف شامل للساحات والمرافق العامة', 4),
(N'جولة تفتيشية', N'جولة تفتيش على النظافة والمرافق', 5),
(N'صيانة طرق', N'فحص وصيانة الطرق والأرصفة', 3),
(N'ري مسطحات خضراء', N'ري الأشجار والمسطحات الخضراء', 4),
(N'تقليم أشجار', N'تقليم وتنظيف الأشجار على جوانب الطرق', 4);

WHILE @taskIdx <= 148
BEGIN
    SELECT @taskUserId = UserId, @taskSupervisor = SupervisorId
    FROM [Users] WHERE Username = 'worker' + CAST(@taskIdx AS VARCHAR(10)) AND Status = 0;

    IF @taskUserId IS NOT NULL AND @taskIdx NOT IN (4,5,6,7,8,9,10) -- W4-W10 already have tasks
    BEGIN
        -- Get worker's zone
        SELECT TOP 1 @taskZone = uz.ZoneId FROM [UserZones] uz WHERE uz.UserId = @taskUserId AND uz.IsActive = 1;
        IF @taskZone IS NULL SET @taskZone = @Z1;
        SELECT @taskLat = CenterLatitude, @taskLng = CenterLongitude FROM [Zones] WHERE ZoneId = @taskZone;
        IF @taskLat IS NULL BEGIN SET @taskLat = 31.900; SET @taskLng = 35.212; END;

        -- Task title based on worker index
        DECLARE @tTitle NVARCHAR(200), @tDesc NVARCHAR(500), @tType INT;
        DECLARE @tIdx INT = ((@taskIdx - 1) % 8) + 1;
        SELECT @tTitle = Title, @tDesc = Desc_, @tType = TaskType FROM @taskTitles WHERE idx = @tIdx;

        DECLARE @zoneName NVARCHAR(100);
        SELECT @zoneName = ZoneName FROM [Zones] WHERE ZoneId = @taskZone;

        -- Task 1: Pending, due today
        INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, LocationDescription, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
        VALUES (@MunicipalityId, @tTitle + N' - ' + @zoneName, @tDesc, @taskZone, @taskUserId, @taskSupervisor,
            ABS(CHECKSUM(NEWID())) % 3, -- priority 0-2
            0, @tType,
            @taskLat + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
            @taskLng + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
            50, @zoneName + N' - موقع العمل',
            CAST(CAST(@Now AS DATE) AS DATETIME), DATEADD(DAY,-1,@Now), NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, DATEADD(DAY,-1,@Now));

        -- Task 2: Pending, due tomorrow
        SET @tIdx = ((@taskIdx) % 8) + 1;
        SELECT @tTitle = Title, @tDesc = Desc_, @tType = TaskType FROM @taskTitles WHERE idx = @tIdx;

        INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, LocationDescription, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
        VALUES (@MunicipalityId, @tTitle + N' - ' + @zoneName, @tDesc, @taskZone, @taskUserId, @taskSupervisor,
            ABS(CHECKSUM(NEWID())) % 3,
            0, @tType,
            @taskLat + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
            @taskLng + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
            50, @zoneName + N' - موقع العمل',
            DATEADD(DAY,1,CAST(CAST(@Now AS DATE) AS DATETIME)), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 1, @Now);

        -- Every 3rd worker also gets a completed task (historical)
        IF @taskIdx % 3 = 0
        BEGIN
            SET @tIdx = ((@taskIdx + 1) % 8) + 1;
            SELECT @tTitle = Title, @tDesc = Desc_, @tType = TaskType FROM @taskTitles WHERE idx = @tIdx;

            INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, LocationDescription, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
            VALUES (@MunicipalityId, @tTitle + N' - ' + @zoneName, @tDesc + N' (مكتمل)', @taskZone, @taskUserId, @taskSupervisor,
                1, 3, @tType, -- Completed
                @taskLat + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                @taskLng + (ABS(CHECKSUM(NEWID())) % 10) * 0.0001,
                50, @zoneName + N' - موقع العمل',
                DATEADD(DAY,-7,@Now), DATEADD(DAY,-8,@Now), DATEADD(DAY,-7,@Now), DATEADD(DAY,-7,@Now), N'تم إنجاز المهمة بنجاح', 100, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, NULL, NULL, 1, 2, DATEADD(DAY,-8,@Now));
        END;
    END;
    SET @taskIdx = @taskIdx + 1;
END;

PRINT 'Tasks assigned to all active workers (2-3 per worker)';

-- 15.5 GPS location points for active workers (3 points each: today)
DECLARE @gpsIdx INT = 4;
DECLARE @gpsUserId INT;
DECLARE @gpsLat FLOAT;
DECLARE @gpsLng FLOAT;

WHILE @gpsIdx <= 148
BEGIN
    SELECT @gpsUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@gpsIdx AS VARCHAR(10)) AND Status = 0;

    IF @gpsUserId IS NOT NULL
    BEGIN
        -- Get zone center
        SELECT TOP 1 @gpsLat = z.CenterLatitude, @gpsLng = z.CenterLongitude
        FROM [UserZones] uz JOIN [Zones] z ON z.ZoneId = uz.ZoneId
        WHERE uz.UserId = @gpsUserId AND uz.IsActive = 1;
        IF @gpsLat IS NULL BEGIN SET @gpsLat = 31.900; SET @gpsLng = 35.212; END;

        -- 3 GPS points today (2hr ago, 1hr ago, now)
        INSERT INTO [LocationHistories] (UserId, Latitude, Longitude, Speed, Accuracy, Heading, Timestamp, IsSync) VALUES
        (@gpsUserId, @gpsLat + (ABS(CHECKSUM(NEWID())) % 20 - 10) * 0.0001, @gpsLng + (ABS(CHECKSUM(NEWID())) % 20 - 10) * 0.0001, 1.0 + (ABS(CHECKSUM(NEWID())) % 20) * 0.1, 5.0 + (ABS(CHECKSUM(NEWID())) % 10), CAST(ABS(CHECKSUM(NEWID())) % 360 AS FLOAT), DATEADD(HOUR, -2, @Now), 1),
        (@gpsUserId, @gpsLat + (ABS(CHECKSUM(NEWID())) % 20 - 10) * 0.0001, @gpsLng + (ABS(CHECKSUM(NEWID())) % 20 - 10) * 0.0001, 1.0 + (ABS(CHECKSUM(NEWID())) % 20) * 0.1, 5.0 + (ABS(CHECKSUM(NEWID())) % 10), CAST(ABS(CHECKSUM(NEWID())) % 360 AS FLOAT), DATEADD(HOUR, -1, @Now), 1),
        (@gpsUserId, @gpsLat + (ABS(CHECKSUM(NEWID())) % 20 - 10) * 0.0001, @gpsLng + (ABS(CHECKSUM(NEWID())) % 20 - 10) * 0.0001, 0.5 + (ABS(CHECKSUM(NEWID())) % 15) * 0.1, 5.0 + (ABS(CHECKSUM(NEWID())) % 10), CAST(ABS(CHECKSUM(NEWID())) % 360 AS FLOAT), @Now, 1);
    END;
    SET @gpsIdx = @gpsIdx + 1;
END;

PRINT 'GPS location points for all active workers (3 per worker)';

-- 15.6 Notifications for active workers (1-2 each)
DECLARE @notifIdx INT = 4;
DECLARE @notifUserId INT;

WHILE @notifIdx <= 100
BEGIN
    SELECT @notifUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@notifIdx AS VARCHAR(10)) AND Status = 0;
    IF @notifUserId IS NOT NULL
    BEGIN
        INSERT INTO [Notifications] (MunicipalityId, UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, ReadAt) VALUES
        (@MunicipalityId, @notifUserId, N'مهمة جديدة', N'تم تعيين مهمة جديدة لك - يرجى البدء', 1, 0, 1, @Now, @Now, NULL),
        (@MunicipalityId, @notifUserId, N'تذكير بالحضور', N'تم تسجيل حضورك بنجاح', 8, 1, 1, DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now), DATEADD(HOUR,-2,@Now));
    END;
    SET @notifIdx = @notifIdx + 1;
END;

PRINT 'Notifications for all health workers';

-- 15.7 Urgent task for worker3
INSERT INTO [Tasks] (MunicipalityId, Title, Description, ZoneId, AssignedToUserId, AssignedByUserId, Priority, Status, TaskType, Latitude, Longitude, MaxDistanceMeters, DueDate, EventTime, StartedAt, CompletedAt, CompletionNotes, ProgressPercentage, ProgressNotes, IsAutoRejected, RejectionReason, RejectedAt, RejectedByUserId, RejectionLatitude, RejectionLongitude, RejectionDistanceMeters, FailedCompletionAttempts, RequiresPhotoProof, ScheduledAt, SourceIssueId, IsSynced, SyncVersion, CreatedAt)
VALUES (@MunicipalityId, N'إزالة نفايات خطرة - راس الطاحونة', N'تم رصد نفايات طبية خطرة بالقرب من مدرسة راس الطاحونة. يجب إزالتها فوراً قبل بداية الدوام المدرسي', @Z2, @W3, @HealthSup2, 3, 0, 0, 31.9071, 35.2117, 30, CAST(CAST(@Now AS DATE) AS DATETIME), @Now, NULL, NULL, NULL, 0, NULL, 0, NULL, NULL, NULL, NULL, NULL, NULL, 0, 1, @Now, NULL, 1, 1, @Now);

PRINT 'Demo polish complete: ALL users demo-ready';

-- ========================================================================
-- 16. TODAY'S ACTIVITY (make dashboard show real numbers for demo)
-- ========================================================================

-- 16.1 Complete 2 of worker1's tasks TODAY (morning work done)
-- W1-T1: garbage collection → Completed at 8:30
UPDATE [Tasks] SET Status = 3, StartedAt = DATEADD(MINUTE,35,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletedAt = DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletionNotes = N'تم جمع جميع النفايات من حي البصبوص بنجاح', ProgressPercentage = 100
WHERE AssignedToUserId = @W1 AND Title = N'جمع نفايات صباحي - البصبوص' AND Status = 0;

-- W1-T2: street sweeping → Completed at 9:45
UPDATE [Tasks] SET Status = 3, StartedAt = DATEADD(MINUTE,40,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletedAt = DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletionNotes = N'تم كنس جميع الشوارع الفرعية', ProgressPercentage = 100
WHERE AssignedToUserId = @W1 AND Title = N'كنس شوارع فرعية - البصبوص' AND Status = 0;

-- W1-T3: container maintenance → InProgress since 10:00
UPDATE [Tasks] SET Status = 1, StartedAt = DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME)),
    ProgressPercentage = 40, ProgressNotes = N'تم فحص 4 من 10 حاويات'
WHERE AssignedToUserId = @W1 AND Title = N'صيانة حاويات شارع 5' AND Status = 0;

-- 16.2 Complete 2 of worker2's tasks TODAY
-- W2-T1: garbage collection → Completed at 8:50
UPDATE [Tasks] SET Status = 3, StartedAt = DATEADD(MINUTE,30,DATEADD(HOUR,7,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletedAt = DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletionNotes = N'تم جمع جميع النفايات من البصبوص شمال', ProgressPercentage = 100
WHERE AssignedToUserId = @W2 AND Title = N'جمع نفايات - البصبوص شمال' AND Status = 0;

-- W2-T2: school cleaning → Completed at 10:15
UPDATE [Tasks] SET Status = 3, StartedAt = DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME)),
    CompletedAt = DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))),
    CompletionNotes = N'تم تنظيف محيط المدرسة بالكامل', ProgressPercentage = 100
WHERE AssignedToUserId = @W2 AND Title = N'تنظيف محيط المدرسة' AND Status = 0;

-- W2-T3: container maint → InProgress since 10:30
UPDATE [Tasks] SET Status = 1, StartedAt = DATEADD(MINUTE,30,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))),
    ProgressPercentage = 25, ProgressNotes = N'بدأت بفحص الحاويات'
WHERE AssignedToUserId = @W2 AND Title = N'صيانة حاويات الحي الغربي' AND Status = 0;

-- 16.3 Complete some bulk worker tasks TODAY (every 5th worker from 11-100)
DECLARE @todayIdx INT = 11;
DECLARE @todayUserId INT;
WHILE @todayIdx <= 100
BEGIN
    SELECT @todayUserId = UserId FROM [Users] WHERE Username = 'worker' + CAST(@todayIdx AS VARCHAR(10)) AND Status = 0;
    IF @todayUserId IS NOT NULL
    BEGIN
        -- Complete their first pending task today
        UPDATE TOP (1) [Tasks] SET Status = 3,
            StartedAt = DATEADD(MINUTE, ABS(CHECKSUM(NEWID())) % 60, DATEADD(HOUR, 7, CAST(CAST(@Now AS DATE) AS DATETIME))),
            CompletedAt = DATEADD(MINUTE, ABS(CHECKSUM(NEWID())) % 60, DATEADD(HOUR, 8, CAST(CAST(@Now AS DATE) AS DATETIME))),
            CompletionNotes = N'تم إنجاز المهمة بنجاح', ProgressPercentage = 100
        WHERE AssignedToUserId = @todayUserId AND Status = 0;

        -- Start their second pending task
        IF @todayIdx % 10 = 1
        BEGIN
            UPDATE TOP (1) [Tasks] SET Status = 1,
                StartedAt = DATEADD(MINUTE, ABS(CHECKSUM(NEWID())) % 60, DATEADD(HOUR, 9, CAST(CAST(@Now AS DATE) AS DATETIME))),
                ProgressPercentage = 30 + ABS(CHECKSUM(NEWID())) % 50,
                ProgressNotes = N'جاري العمل على المهمة'
            WHERE AssignedToUserId = @todayUserId AND Status = 0;
        END;
    END;
    SET @todayIdx = @todayIdx + 5;
END;

-- 16.4 Today's task completion audit logs
INSERT INTO [AuditLogs] (UserId, Username, Action, Details, IpAddress, UserAgent, CreatedAt)
VALUES
(@W1, 'worker1', 'TaskComplete', N'إكمال مهمة جمع نفايات صباحي - البصبوص', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME)))),
(@W1, 'worker1', 'TaskComplete', N'إكمال مهمة كنس شوارع فرعية - البصبوص', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME)))),
(@W1, 'worker1', 'TaskStart', N'بدء مهمة صيانة حاويات شارع 5', '10.0.0.50', 'FollowUp-Mobile/1.0 (Android)', DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))),
(@W2, 'worker2', 'TaskComplete', N'إكمال مهمة جمع نفايات - البصبوص شمال', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME)))),
(@W2, 'worker2', 'TaskComplete', N'إكمال مهمة تنظيف محيط المدرسة', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME)))),
(@W2, 'worker2', 'TaskStart', N'بدء مهمة صيانة حاويات الحي الغربي', '10.0.0.51', 'FollowUp-Mobile/1.0 (Android)', DATEADD(MINUTE,30,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))));

-- 16.5 Today's completion notifications for supervisors
INSERT INTO [Notifications] (MunicipalityId, UserId, Title, Message, Type, IsRead, IsSent, CreatedAt, SentAt, ReadAt)
VALUES
(@MunicipalityId, @HealthSup1, N'مهمة مكتملة', N'العامل أحمد محمد أكمل مهمة جمع نفايات صباحي - البصبوص', 3, 0, 1, DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), NULL),
(@MunicipalityId, @HealthSup1, N'مهمة مكتملة', N'العامل أحمد محمد أكمل مهمة كنس شوارع فرعية - البصبوص', 3, 0, 1, DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME))), DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME))), NULL),
(@MunicipalityId, @HealthSup1, N'مهمة مكتملة', N'العامل خالد أحمد أكمل مهمة جمع نفايات - البصبوص شمال', 3, 0, 1, DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), NULL),
(@MunicipalityId, @HealthSup1, N'مهمة مكتملة', N'العامل خالد أحمد أكمل مهمة تنظيف محيط المدرسة', 3, 0, 1, DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))), DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))), NULL);

-- 16.6 Photos for today's completed tasks (evidence)
DECLARE @W1CompletedT1 INT, @W1CompletedT2 INT, @W2CompletedT1 INT, @W2CompletedT2 INT;
SELECT TOP 1 @W1CompletedT1 = TaskId FROM [Tasks] WHERE AssignedToUserId = @W1 AND Status = 3 AND Title LIKE N'%نفايات صباحي%';
SELECT TOP 1 @W1CompletedT2 = TaskId FROM [Tasks] WHERE AssignedToUserId = @W1 AND Status = 3 AND Title LIKE N'%كنس شوارع%';
SELECT TOP 1 @W2CompletedT1 = TaskId FROM [Tasks] WHERE AssignedToUserId = @W2 AND Status = 3 AND Title LIKE N'%البصبوص شمال%';
SELECT TOP 1 @W2CompletedT2 = TaskId FROM [Tasks] WHERE AssignedToUserId = @W2 AND Status = 3 AND Title LIKE N'%محيط المدرسة%';

INSERT INTO [Photos] (PhotoUrl, EntityType, EntityId, TaskId, IssueId, OrderIndex, FileSizeBytes, UploadedAt, UploadedByUserId, CreatedAt)
VALUES
('/uploads/tasks/w1_t1_before_today.jpg', 'Task', @W1CompletedT1, @W1CompletedT1, NULL, 0, 256000, DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), @W1, DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w1_t1_after_today.jpg', 'Task', @W1CompletedT1, @W1CompletedT1, NULL, 1, 298000, DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), @W1, DATEADD(MINUTE,30,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w1_t2_before_today.jpg', 'Task', @W1CompletedT2, @W1CompletedT2, NULL, 0, 231000, DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME))), @W1, DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w1_t2_after_today.jpg', 'Task', @W1CompletedT2, @W1CompletedT2, NULL, 1, 275000, DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME))), @W1, DATEADD(MINUTE,45,DATEADD(HOUR,9,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w2_t1_before_today.jpg', 'Task', @W2CompletedT1, @W2CompletedT1, NULL, 0, 245000, DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), @W2, DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w2_t1_after_today.jpg', 'Task', @W2CompletedT1, @W2CompletedT1, NULL, 1, 310000, DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME))), @W2, DATEADD(MINUTE,50,DATEADD(HOUR,8,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w2_t2_before_today.jpg', 'Task', @W2CompletedT2, @W2CompletedT2, NULL, 0, 218000, DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))), @W2, DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME)))),
('/uploads/tasks/w2_t2_after_today.jpg', 'Task', @W2CompletedT2, @W2CompletedT2, NULL, 1, 287000, DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))), @W2, DATEADD(MINUTE,15,DATEADD(HOUR,10,CAST(CAST(@Now AS DATE) AS DATETIME))));

PRINT 'Today activity: 4 tasks completed (W1:2, W2:2), 2 in-progress, ~18 bulk completions, audit logs, notifications, photos';

-- ========================================================================
-- SUMMARY
-- ========================================================================
PRINT '========================================';
PRINT 'AL-BIREH MUNICIPALITY SEED DATA COMPLETE';
PRINT '========================================';
PRINT '';
PRINT 'Municipality: Al-Bireh';
PRINT 'Zones: 21 (20 real Al-Bireh neighborhoods + BZU)';
PRINT 'Departments: 3 (Health, Works, Agriculture)';
PRINT '';
PRINT 'USERS (155 total, 3 inactive/suspended):';
PRINT '  Admin: 1 (admin)';
PRINT '  Supervisors: 8 (super1-super8)';
PRINT '    super1-5 = Health, super6-7 = Works, super8 = Agri';
PRINT '    super4 = Inactive';
PRINT '  Workers: 148 (worker1-worker148)';
PRINT '    Health: 100 (worker1-100), Works: 30 (worker101-130), Agri: 18 (worker131-148)';
PRINT '    worker120 = Inactive, worker145 = Suspended';
PRINT '';
PRINT 'DEMO ACCOUNTS (all checked in today, active):';
PRINT '  admin  / pass123@  -- Admin';
PRINT '  super1 / pass123@  -- Supervisor (manages worker1+worker2)';
PRINT '  super2 / pass123@  -- Supervisor (manages worker3)';
PRINT '  worker1/ pass123@  -- 6 NEW Pending tasks (Z1+BZU) under super1';
PRINT '  worker2/ pass123@  -- 6 NEW Pending tasks (Z1+BZU) under super1';
PRINT '  worker3/ pass123@  -- 8 tasks VARIETY of statuses incl. 1 URGENT (Z2+BZU) under super2';
PRINT '';
PRINT 'TASK OVERVIEW:';
PRINT '  worker1: 2 completed TODAY, 1 in-progress, 3 pending';
PRINT '  worker2: 2 completed TODAY, 1 in-progress, 3 pending';
PRINT '  worker3: 2 completed, 1 under-review, 1 rejected, 1 in-progress, 2 pending, 1 URGENT pending';
PRINT '  Other workers (W4-W10): 3 completed, 2 in-progress, 2 pending, 1 rejected, 1 under-review, 1 cancelled';
PRINT '';
PRINT 'ATTENDANCE:';
PRINT '  All 3 demo workers: ~20 historical work days + checked in TODAY';
PRINT '  worker1: OnTime (7:00-7:10)';
PRINT '  worker2: Sometimes late (7:00-7:35)';
PRINT '  worker3: Mostly on time (7:00-7:20)';
PRINT '';
PRINT 'OTHER DATA:';
PRINT '  Issues: 8 (3 W1 + 2 W2 + 3 W3)';
PRINT '  Notifications: 24 (4 W1 + 4 W2 + 5 W3 + 4 super1 + 4 super2 + 3 admin)';
PRINT '  Appeals: 2 for worker3 (1 pending + 1 approved)';
PRINT '  Audit Logs: ~40 entries';
PRINT '  Location History: 39 GPS points (15 W1 + 12 W2 + 12 W3)';
PRINT '  Photos: 8 (4 task + 4 issue)';
PRINT '  Task Templates: 10 (4 daily, 3 weekly, 1 monthly, 1 inactive, 1 BZU)';
PRINT '  Teams: 11 (6 Works + 5 Agriculture)';
PRINT '  Zone Assignments: 100 health + 3 BZU for demo workers';
PRINT '';
PRINT 'BZU ZONE: جامعة بيرزيت (31.9554, 35.1751) assigned to W1, W2, W3';
PRINT '========================================';
PRINT 'ALL CREDENTIALS: password = pass123@';
PRINT 'SEED DATA COMPLETE';

-- ========================================================================
-- DEMO SANITY REPORT (read-only queries to verify seed state)
-- ========================================================================
PRINT '';
PRINT '--- DEMO SANITY REPORT ---';
PRINT '';

-- 1. Demo users exist and are active
SELECT 'Demo Users' AS Section, Username, Role, Status
FROM [Users]
WHERE Username IN ('admin','super1','super2','worker1','worker2','worker3')
ORDER BY Role, Username;

-- 2. Today's attendance for demo workers
SELECT 'Today Attendance' AS Section, u.Username, a.Status AS AttStatus,
       a.CheckInEventTime, a.CheckOutEventTime, a.AttendanceType
FROM [Attendances] a
JOIN [Users] u ON u.UserId = a.UserId
WHERE CAST(a.CheckInEventTime AS date) = CAST(@Now AS date)
  AND u.Username IN ('worker1','worker2','worker3')
ORDER BY a.CheckInEventTime;

-- 3. Task count by status
SELECT 'Task Count by Status' AS Section, Status, COUNT(*) AS Cnt
FROM [Tasks]
GROUP BY Status
ORDER BY Status;

-- 3b. Today's completed tasks
SELECT 'Today Completed Tasks' AS Section, u.Username, t.Title, t.CompletedAt
FROM [Tasks] t
JOIN [Users] u ON u.UserId = t.AssignedToUserId
WHERE t.Status = 3 AND CAST(t.CompletedAt AS DATE) = CAST(@Now AS DATE)
ORDER BY t.CompletedAt;

-- 4. Latest GPS per demo worker
SELECT 'Latest GPS' AS Section, u.Username, lh.Timestamp, lh.Latitude, lh.Longitude
FROM [LocationHistories] lh
JOIN [Users] u ON u.UserId = lh.UserId
WHERE u.Username IN ('worker1','worker2','worker3')
  AND lh.Timestamp = (
      SELECT MAX(lh2.Timestamp)
      FROM [LocationHistories] lh2
      WHERE lh2.UserId = lh.UserId
  )
ORDER BY u.Username;

-- 5. Unread notifications per demo user
SELECT 'Unread Notifications' AS Section, u.Username, COUNT(*) AS Unread
FROM [Notifications] n
JOIN [Users] u ON u.UserId = n.UserId
WHERE n.IsRead = 0
  AND u.Username IN ('admin','super1','super2','worker1','worker2','worker3')
GROUP BY u.Username
ORDER BY Unread DESC;

-- 6. Zone assignments for demo workers
SELECT 'Zone Assignments' AS Section, u.Username, z.ZoneName, z.ZoneCode
FROM [UserZones] uz
JOIN [Users] u ON u.UserId = uz.UserId
JOIN [Zones] z ON z.ZoneId = uz.ZoneId
WHERE u.Username IN ('worker1','worker2','worker3')
ORDER BY u.Username, z.ZoneName;

-- 7. Supervisor-Worker mapping for demo accounts
SELECT 'Supervisor Mapping' AS Section, w.Username AS Worker, s.Username AS Supervisor
FROM [Users] w
JOIN [Users] s ON w.SupervisorId = s.UserId
WHERE w.Username IN ('worker1','worker2','worker3')
ORDER BY s.Username, w.Username;

PRINT '';
PRINT '--- END SANITY REPORT ---';
