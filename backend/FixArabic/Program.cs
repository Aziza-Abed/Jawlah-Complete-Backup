using Microsoft.Data.SqlClient;
using System.Text.Json;

var connectionString = "Server=(localdb)\\JawlahTest;Database=FollowUp;Trusted_Connection=True;TrustServerCertificate=True;";

using var connection = new SqlConnection(connectionString);
connection.Open();

Console.WriteLine("=== FIXING ALL DATA ===\n");

// 1. Fix Arabic text for users, departments, teams
Console.WriteLine("1. Fixing Arabic text...");
var textCommands = new[]
{
    "UPDATE Municipalities SET Name = N'بلدية البيرة', NameEnglish = 'Al-Bireh Municipality' WHERE MunicipalityId = 1",
    "UPDATE Departments SET Name = N'قسم الصحة والنظافة', NameEnglish = 'Health & Sanitation' WHERE Code = 'HEALTH'",
    "UPDATE Departments SET Name = N'قسم الأشغال العامة', NameEnglish = 'Public Works' WHERE Code = 'WORKS'",
    "UPDATE Departments SET Name = N'قسم الزراعة والحدائق', NameEnglish = 'Agriculture & Parks' WHERE Code = 'AGRI'",
    "UPDATE Users SET FullName = N'مدير النظام' WHERE Username = 'admin'",
    "UPDATE Users SET FullName = N'أحمد محمود' WHERE Username = 'health_sup1'",
    "UPDATE Users SET FullName = N'محمد خالد' WHERE Username = 'health_sup2'",
    "UPDATE Users SET FullName = N'سامي علي' WHERE Username = 'health_sup3'",
    "UPDATE Users SET FullName = N'عمر حسن' WHERE Username = 'health_sup4'",
    "UPDATE Users SET FullName = N'يوسف أحمد' WHERE Username = 'health_sup5'",
    "UPDATE Users SET FullName = N'خالد سعيد' WHERE Username = 'works_sup1'",
    "UPDATE Users SET FullName = N'فادي محمد' WHERE Username = 'works_sup2'",
    "UPDATE Users SET FullName = N'ماهر حسين' WHERE Username = 'agri_sup1'",
    "UPDATE Teams SET Name = N'فريق البناء والتشييد' WHERE Code = 'BUILD1'",
    "UPDATE Teams SET Name = N'فريق الإنارة والكهرباء' WHERE Code = 'LIGHT1'",
    "UPDATE Teams SET Name = N'فريق الصيانة العامة' WHERE Code = 'MAINT1'",
    "UPDATE Teams SET Name = N'فريق الطوارئ' WHERE Code = 'EMERG1'",
    "UPDATE Teams SET Name = N'فريق البنية التحتية' WHERE Code = 'INFRA1'",
    "UPDATE Teams SET Name = N'فريق الطرق' WHERE Code = 'ROAD1'",
    "UPDATE Teams SET Name = N'فريق الحدائق العامة' WHERE Code = 'GARDEN1'",
    "UPDATE Teams SET Name = N'فريق التشجير' WHERE Code = 'TREE1'",
    "UPDATE Teams SET Name = N'فريق الري' WHERE Code = 'IRRIG1'",
    "UPDATE Teams SET Name = N'فريق المشاتل' WHERE Code = 'NURSE1'",
    "UPDATE Teams SET Name = N'فريق صيانة الحدائق' WHERE Code = 'GMAINT1'"
};
foreach (var sql in textCommands)
{
    using var cmd = new SqlCommand(sql, connection);
    cmd.ExecuteNonQuery();
}

// Update workers with realistic Arabic names
var firstNames = new[] { "محمد", "أحمد", "علي", "حسن", "حسين", "عمر", "خالد", "سعيد", "يوسف", "إبراهيم",
    "عبدالله", "عبدالرحمن", "فادي", "رامي", "سامر", "باسم", "وليد", "طارق", "زياد", "مازن",
    "نبيل", "جمال", "كمال", "فيصل", "سليم", "راشد", "ماجد", "فهد", "سالم", "ناصر",
    "هاني", "رائد", "عماد", "أيمن", "بلال", "أنس", "معاذ", "ياسر", "ثامر", "منصور" };
var lastNames = new[] { "العمري", "الشريف", "الحسن", "الخالدي", "السعيد", "الأحمد", "المحمود", "الناصر",
    "البيروتي", "الرفاعي", "القاسم", "الصالح", "العبدالله", "الحمد", "الفارس", "الشمالي",
    "الجنوبي", "الشرقي", "الغربي", "التميمي", "الزعبي", "النجار", "الحداد", "البنا",
    "الصباغ", "العطار", "الخياط", "الحلاق", "السمان", "الزيتوني" };

var nameRandom = new Random(42); // Fixed seed for reproducibility
var allWorkersList = new List<(int Id, string Username)>();
using (var cmd = new SqlCommand("SELECT UserId, Username FROM Users WHERE Role = 2 ORDER BY UserId", connection))
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
        allWorkersList.Add((reader.GetInt32(0), reader.GetString(1)));
}

foreach (var w in allWorkersList)
{
    var firstName = firstNames[nameRandom.Next(firstNames.Length)];
    var lastName = lastNames[nameRandom.Next(lastNames.Length)];
    var fullName = $"{firstName} {lastName}";

    using var cmd = new SqlCommand("UPDATE Users SET FullName = @Name WHERE UserId = @Id", connection);
    cmd.Parameters.AddWithValue("@Name", fullName);
    cmd.Parameters.AddWithValue("@Id", w.Id);
    cmd.ExecuteNonQuery();
}
Console.WriteLine($"   Updated {allWorkersList.Count} workers with real names!");

// 2. Clear old zones and import from GIS (using Blocks)
Console.WriteLine("2. Importing zones from GIS (Blocks)...");

// Read the Blocks GeoJSON file
var geoJsonPath = @"C:\Users\hp\Documents\FollowUp\Jawlah-Repo\GIS\Blocks_WGS84.geojson";
var geoJson = File.ReadAllText(geoJsonPath);
var doc = JsonDocument.Parse(geoJson);
var features = doc.RootElement.GetProperty("features");

// Delete dependent data first, then zones
Console.WriteLine("   Clearing old data...");
using (var cmd = new SqlCommand("DELETE FROM Issues", connection))
    cmd.ExecuteNonQuery();
using (var cmd = new SqlCommand("DELETE FROM Tasks", connection))
    cmd.ExecuteNonQuery();
using (var cmd = new SqlCommand("DELETE FROM Attendances", connection))
    cmd.ExecuteNonQuery();
using (var cmd = new SqlCommand("DELETE FROM UserZones", connection))
    cmd.ExecuteNonQuery();
using (var cmd = new SqlCommand("DELETE FROM Zones", connection))
    cmd.ExecuteNonQuery();

Console.WriteLine("   Importing blocks from GeoJSON...");
int zoneCount = 0;

foreach (var feature in features.EnumerateArray())
{
    var props = feature.GetProperty("properties");
    var blockNum = props.GetProperty("BlockNumbe").GetString() ?? "";
    var blockNameAr = props.GetProperty("BlockName_").GetString() ?? "";
    var blockNameEn = props.GetProperty("BlockName1").GetString() ?? "";

    // Get actual area from SHAPE_Area
    double area = 50000;
    if (props.TryGetProperty("SHAPE_Area", out var areaVal))
        area = areaVal.GetDouble();

    // Calculate center from all coordinates (centroid approximation)
    var geometry = feature.GetProperty("geometry");
    var coords = geometry.GetProperty("coordinates");
    double sumLat = 0, sumLng = 0;
    int pointCount = 0;

    try
    {
        // MultiPolygon: coords[polygon][ring][point][lng/lat]
        foreach (var polygon in coords.EnumerateArray())
        {
            foreach (var ring in polygon.EnumerateArray())
            {
                foreach (var point in ring.EnumerateArray())
                {
                    sumLng += point[0].GetDouble();
                    sumLat += point[1].GetDouble();
                    pointCount++;
                }
            }
        }
    }
    catch { }

    double centerLat = pointCount > 0 ? sumLat / pointCount : 31.9;
    double centerLng = pointCount > 0 ? sumLng / pointCount : 35.2;

    // Create zone code from block number
    var zoneCode = $"B{blockNum.PadLeft(2, '0')}";

    // Clean up block name - remove the number suffix like "(1)", "(2)" etc.
    var displayName = System.Text.RegularExpressions.Regex.Replace(blockNameAr, @"\(\d+\)$", "").Trim();

    var sql = @"INSERT INTO Zones (ZoneCode, ZoneName, Description, CenterLatitude, CenterLongitude,
                IsActive, MunicipalityId, CreatedAt, BoundaryGeoJson, AreaSquareMeters, Version, VersionDate)
                VALUES (@Code, @Name, @Desc, @Lat, @Lng, 1, 1, GETUTCDATE(), @GeoJson, @Area, 1, GETUTCDATE())";

    using var cmd = new SqlCommand(sql, connection);
    cmd.Parameters.AddWithValue("@Code", zoneCode);
    cmd.Parameters.AddWithValue("@Name", displayName);
    cmd.Parameters.AddWithValue("@Desc", blockNameEn);
    cmd.Parameters.AddWithValue("@Lat", centerLat);
    cmd.Parameters.AddWithValue("@Lng", centerLng);
    cmd.Parameters.AddWithValue("@GeoJson", feature.GetProperty("geometry").ToString());
    cmd.Parameters.AddWithValue("@Area", area);

    try { cmd.ExecuteNonQuery(); zoneCount++; }
    catch (Exception ex) { Console.WriteLine($"   Error inserting block {zoneCode}: {ex.Message}"); }
}
Console.WriteLine($"   Imported {zoneCount} blocks as zones!");

// 3. Assign workers to zones
Console.WriteLine("3. Assigning workers to zones...");
var assignSql = @"
    -- Get all zone IDs
    DECLARE @ZoneIds TABLE (ZoneId INT, RowNum INT);
    INSERT INTO @ZoneIds SELECT ZoneId, ROW_NUMBER() OVER (ORDER BY ZoneId) FROM Zones;

    -- Get total zones
    DECLARE @TotalZones INT = (SELECT COUNT(*) FROM @ZoneIds);

    -- Assign health workers to zones (distribute evenly)
    INSERT INTO UserZones (UserId, ZoneId, AssignedAt, IsActive, AssignedByUserId)
    SELECT u.UserId,
           z.ZoneId,
           GETUTCDATE(), 1, 1
    FROM Users u
    CROSS APPLY (
        SELECT ZoneId FROM @ZoneIds
        WHERE RowNum = ((u.UserId % @TotalZones) + 1)
    ) z
    WHERE u.Role = 2
    AND NOT EXISTS (SELECT 1 FROM UserZones uz WHERE uz.UserId = u.UserId);
";
using (var cmd = new SqlCommand(assignSql, connection))
    cmd.ExecuteNonQuery();
Console.WriteLine("   Done!");

// 4. Clear old tasks and add new ones for each supervisor
Console.WriteLine("4. Adding tasks for all supervisors...");

// Clear existing tasks
using (var cmd = new SqlCommand("DELETE FROM Tasks", connection))
    cmd.ExecuteNonQuery();

// Get supervisor IDs and their workers
var supervisors = new List<(int Id, string Username)>();
using (var cmd = new SqlCommand("SELECT UserId, Username FROM Users WHERE Role = 1", connection))
using (var reader = cmd.ExecuteReader())
{
    while (reader.Read())
        supervisors.Add((reader.GetInt32(0), reader.GetString(1)));
}

// TaskStatus: Pending=0, InProgress=1, Completed=2, Cancelled=3, Approved=4
var taskTypes = new (string title, string desc, int status)[]
{
    ("مهمة تنظيف شوارع", "تنظيف الشوارع الرئيسية والفرعية في المنطقة", 0), // Pending
    ("مهمة جمع نفايات", "جمع النفايات من الحاويات وتنظيف المنطقة", 1), // InProgress
    ("مهمة صيانة أرصفة", "إصلاح وصيانة الأرصفة المتضررة", 2), // Completed
    ("مهمة تقليم أشجار", "تقليم الأشجار على جوانب الطرق", 2), // Completed
    ("مهمة إصلاح إنارة", "إصلاح أعمدة الإنارة المعطلة", 4), // Approved
    ("مهمة ري حدائق", "ري الحدائق العامة والمساحات الخضراء", 4), // Approved
    ("مهمة نظافة حديقة", "تنظيف الحديقة العامة وصيانة المرافق", 1), // InProgress
    ("مهمة طلاء أرصفة", "طلاء حواف الأرصفة باللونين الأبيض والأسود", 2), // Completed
    ("مهمة إزالة مخلفات", "إزالة مخلفات البناء من الشارع", 4), // Approved
    ("مهمة تنظيف حاويات", "تنظيف وتعقيم حاويات النفايات", 2) // Completed
};

int taskCount = 0;
foreach (var sup in supervisors)
{
    // Get workers under this supervisor
    var workers = new List<int>();
    using (var cmd = new SqlCommand($"SELECT UserId FROM Users WHERE SupervisorId = {sup.Id}", connection))
    using (var reader = cmd.ExecuteReader())
    {
        while (reader.Read())
            workers.Add(reader.GetInt32(0));
    }

    // If no workers assigned, get some based on username pattern
    if (workers.Count == 0)
    {
        var pattern = sup.Username.Contains("health") ? "health_w%" :
                      sup.Username.Contains("works") ? "works_w%" : "agri_w%";
        using var cmd = new SqlCommand($"SELECT TOP 20 UserId FROM Users WHERE Username LIKE '{pattern}'", connection);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
            workers.Add(reader.GetInt32(0));
    }

    // Add multiple tasks for each worker (2-3 tasks each for top performers data)
    var random = new Random(sup.Id);
    foreach (var workerId in workers.Take(20))
    {
        // Add 2-3 tasks per worker
        int tasksPerWorker = random.Next(2, 4);
        for (int t = 0; t < tasksPerWorker; t++)
        {
            var (title, desc, status) = taskTypes[random.Next(taskTypes.Length)];
            var hoursAgo = random.Next(1, 6);

            var sql = @"
                INSERT INTO Tasks (AssignedToUserId, AssignedByUserId, ZoneId, Title, Description,
                    Priority, Status, CreatedAt, DueDate, EventTime, SyncTime, IsSynced, SyncVersion,
                    TaskType, RequiresPhotoProof, MunicipalityId, IsTeamTask,
                    StartedAt, CompletedAt, CompletionNotes)
                SELECT @WorkerId, @SupId,
                    (SELECT TOP 1 ZoneId FROM UserZones WHERE UserId = @WorkerId),
                    @Title, @Desc,
                    @Priority, @Status,
                    DATEADD(hour, -@Hours, GETUTCDATE()),
                    DATEADD(day, 1, GETUTCDATE()),
                    GETUTCDATE(), GETUTCDATE(), 1, 1, 0, 0, 1, 0,
                    CASE WHEN @Status > 0 THEN DATEADD(hour, -(@Hours - 1), GETUTCDATE()) ELSE NULL END,
                    CASE WHEN @Status IN (2, 4) THEN DATEADD(minute, -@Minutes, GETUTCDATE()) ELSE NULL END,
                    CASE WHEN @Status IN (2, 4) THEN N'تم إنجاز المهمة بنجاح' ELSE NULL END";

            using var cmd = new SqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@WorkerId", workerId);
            cmd.Parameters.AddWithValue("@SupId", sup.Id);
            cmd.Parameters.AddWithValue("@Title", title);
            cmd.Parameters.AddWithValue("@Desc", desc);
            cmd.Parameters.AddWithValue("@Status", status);
            cmd.Parameters.AddWithValue("@Priority", random.Next(1, 4));
            cmd.Parameters.AddWithValue("@Hours", hoursAgo);
            cmd.Parameters.AddWithValue("@Minutes", random.Next(10, 180)); // Completed within last 3 hours

            try { cmd.ExecuteNonQuery(); taskCount++; }
            catch { }
        }
    }
}
Console.WriteLine($"   Added {taskCount} tasks!");

// 5. Ensure attendance data - most workers present, only 5 workers + 2 supervisors absent
Console.WriteLine("5. Adding attendance records...");
var attendanceSql = @"
    DELETE FROM Attendances;

    -- Get zone IDs for distribution
    DECLARE @ZoneList TABLE (ZoneId INT, RowNum INT);
    INSERT INTO @ZoneList SELECT ZoneId, ROW_NUMBER() OVER (ORDER BY ZoneId) FROM Zones;
    DECLARE @TotalZones INT = (SELECT COUNT(*) FROM @ZoneList);

    -- Get 5 random workers to be absent
    DECLARE @AbsentWorkers TABLE (UserId INT);
    INSERT INTO @AbsentWorkers SELECT TOP 5 UserId FROM Users WHERE Role = 2 ORDER BY NEWID();

    -- Get 2 random supervisors to be absent
    DECLARE @AbsentSupervisors TABLE (UserId INT);
    INSERT INTO @AbsentSupervisors SELECT TOP 2 UserId FROM Users WHERE Role = 1 ORDER BY NEWID();

    -- Get today's start time for check-ins (between 6 AM and now)
    DECLARE @TodayStart DATETIME = CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME);
    DECLARE @NowMinutes INT = DATEDIFF(minute, @TodayStart, GETUTCDATE());
    -- Ensure we have at least 60 minutes to work with (for early morning runs)
    IF @NowMinutes < 60 SET @NowMinutes = 60;

    -- Add attendance for ALL workers except 5 absent ones (143 workers present)
    INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime,
        CheckInLatitude, CheckInLongitude, Status, IsValidated, ValidationMessage,
        IsSynced, SyncVersion, MunicipalityId, AttendanceType, ApprovalStatus, IsManual)
    SELECT u.UserId,
        COALESCE((SELECT TOP 1 ZoneId FROM UserZones WHERE UserId = u.UserId),
                 (SELECT ZoneId FROM @ZoneList WHERE RowNum = ((u.UserId % @TotalZones) + 1))),
        DATEADD(minute, ABS(CHECKSUM(NEWID())) % @NowMinutes, @TodayStart),
        GETUTCDATE(),
        31.9 + (RAND(CHECKSUM(NEWID())) * 0.05),
        35.2 + (RAND(CHECKSUM(NEWID())) * 0.05),
        1, 1, N'تم التحقق بنجاح', 1, 1, 1, 0, 1, 0
    FROM Users u
    WHERE u.Role = 2
    AND u.UserId NOT IN (SELECT UserId FROM @AbsentWorkers);

    -- Add attendance for supervisors except 2 absent ones (6 supervisors present)
    INSERT INTO Attendances (UserId, ZoneId, CheckInEventTime, CheckInSyncTime,
        CheckInLatitude, CheckInLongitude, Status, IsValidated, ValidationMessage,
        IsSynced, SyncVersion, MunicipalityId, AttendanceType, ApprovalStatus, IsManual)
    SELECT u.UserId,
        (SELECT TOP 1 ZoneId FROM Zones ORDER BY NEWID()),
        DATEADD(minute, ABS(CHECKSUM(NEWID())) % @NowMinutes, @TodayStart),
        GETUTCDATE(),
        31.9 + (RAND(CHECKSUM(NEWID())) * 0.01),
        35.2 + (RAND(CHECKSUM(NEWID())) * 0.01),
        1, 1, N'تم التحقق بنجاح', 1, 1, 1, 0, 1, 0
    FROM Users u
    WHERE u.Role = 1
    AND u.UserId NOT IN (SELECT UserId FROM @AbsentSupervisors);
";
using (var cmd = new SqlCommand(attendanceSql, connection))
    cmd.ExecuteNonQuery();
Console.WriteLine("   Done!");

// 6. Add issues with all statuses
Console.WriteLine("6. Adding issues with all statuses...");
var issuesSql = @"
    DELETE FROM Issues;

    -- Get zone IDs for issues
    DECLARE @ZoneIds TABLE (ZoneId INT, RowNum INT);
    INSERT INTO @ZoneIds SELECT TOP 10 ZoneId, ROW_NUMBER() OVER (ORDER BY ZoneId) FROM Zones;

    -- Get worker IDs
    DECLARE @WorkerIds TABLE (UserId INT, RowNum INT);
    INSERT INTO @WorkerIds SELECT TOP 20 UserId, ROW_NUMBER() OVER (ORDER BY UserId) FROM Users WHERE Role = 2;

    -- Reported issues (Status = 1) - New
    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion)
    SELECT 1, w.UserId, z.ZoneId,
        N'حفرة في الشارع الرئيسي', N'وجود حفرة كبيرة تشكل خطراً على المارة والسيارات',
        1, 3, 1, 31.9 + (w.RowNum * 0.001), 35.2 + (w.RowNum * 0.001), N'بالقرب من المدرسة',
        DATEADD(hour, -w.RowNum, GETUTCDATE()), GETUTCDATE(), 1, 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum <= 3 AND z.RowNum = 1;

    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion)
    SELECT 1, w.UserId, z.ZoneId,
        N'تراكم نفايات في الحي', N'تراكم كبير للنفايات يسبب روائح كريهة ويجذب الحشرات',
        3, 2, 1, 31.91 + (w.RowNum * 0.001), 35.21 + (w.RowNum * 0.001), N'خلف المجمع التجاري',
        DATEADD(hour, -w.RowNum - 5, GETUTCDATE()), GETUTCDATE(), 1, 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum BETWEEN 4 AND 6 AND z.RowNum = 2;

    -- UnderReview issues (Status = 2) - قيد المراجعة
    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion)
    SELECT 1, w.UserId, z.ZoneId,
        N'عمود إنارة معطل', N'عمود الإنارة لا يعمل منذ أسبوع مما يسبب ظلام في المنطقة',
        1, 2, 2, 31.92 + (w.RowNum * 0.001), 35.22 + (w.RowNum * 0.001), N'مقابل المسجد',
        DATEADD(day, -2, GETUTCDATE()), GETUTCDATE(), 1, 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum BETWEEN 7 AND 9 AND z.RowNum = 3;

    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion)
    SELECT 1, w.UserId, z.ZoneId,
        N'تسرب مياه من أنبوب', N'تسرب مياه كبير يسبب هدر وتجمع مياه في الشارع',
        1, 3, 2, 31.93 + (w.RowNum * 0.001), 35.23 + (w.RowNum * 0.001), N'شارع المدينة',
        DATEADD(day, -1, GETUTCDATE()), GETUTCDATE(), 1, 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum BETWEEN 10 AND 12 AND z.RowNum = 4;

    -- Resolved issues (Status = 3) - مغلقة
    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion,
        ResolvedAt, ResolutionNotes, ResolvedByUserId)
    SELECT 1, w.UserId, z.ZoneId,
        N'شجرة متساقطة على الرصيف', N'شجرة كبيرة سقطت وتسد الرصيف بالكامل',
        2, 4, 3, 31.94 + (w.RowNum * 0.001), 35.24 + (w.RowNum * 0.001), N'حديقة البلدية',
        DATEADD(day, -5, GETUTCDATE()), GETUTCDATE(), 1, 1,
        DATEADD(day, -3, GETUTCDATE()), N'تم إزالة الشجرة وتنظيف المنطقة', 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum BETWEEN 13 AND 15 AND z.RowNum = 5;

    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion,
        ResolvedAt, ResolutionNotes, ResolvedByUserId)
    SELECT 1, w.UserId, z.ZoneId,
        N'رصيف مكسور', N'رصيف متضرر يشكل خطراً على المشاة خاصة كبار السن',
        1, 2, 3, 31.95 + (w.RowNum * 0.001), 35.25 + (w.RowNum * 0.001), N'أمام البنك',
        DATEADD(day, -7, GETUTCDATE()), GETUTCDATE(), 1, 1,
        DATEADD(day, -4, GETUTCDATE()), N'تم إصلاح الرصيف بالكامل', 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum BETWEEN 16 AND 18 AND z.RowNum = 6;

    -- Dismissed issues (Status = 4)
    INSERT INTO Issues (MunicipalityId, ReportedByUserId, ZoneId, Title, Description, Type, Severity, Status,
        Latitude, Longitude, LocationDescription, ReportedAt, EventTime, IsSynced, SyncVersion,
        ResolvedAt, ResolutionNotes, ResolvedByUserId)
    SELECT 1, w.UserId, z.ZoneId,
        N'طلب إنارة إضافية', N'طلب إضافة أعمدة إنارة في منطقة مضاءة بشكل كافي',
        1, 1, 4, 31.96, 35.26, N'شارع فرعي',
        DATEADD(day, -10, GETUTCDATE()), GETUTCDATE(), 1, 1,
        DATEADD(day, -8, GETUTCDATE()), N'المنطقة مضاءة بشكل كافي حالياً', 1
    FROM @WorkerIds w CROSS JOIN @ZoneIds z WHERE w.RowNum = 19 AND z.RowNum = 7;
";
using (var cmd = new SqlCommand(issuesSql, connection))
    cmd.ExecuteNonQuery();

// Count issues by status
var issueStats = "";
using (var cmd = new SqlCommand(@"
    SELECT
        (SELECT COUNT(*) FROM Issues WHERE Status = 1) as Reported,
        (SELECT COUNT(*) FROM Issues WHERE Status = 2) as UnderReview,
        (SELECT COUNT(*) FROM Issues WHERE Status = 3) as Resolved,
        (SELECT COUNT(*) FROM Issues WHERE Status = 4) as Dismissed
", connection))
using (var reader = cmd.ExecuteReader())
{
    if (reader.Read())
    {
        issueStats = $"   Reported: {reader.GetInt32(0)}, UnderReview: {reader.GetInt32(1)}, Resolved: {reader.GetInt32(2)}, Dismissed: {reader.GetInt32(3)}";
    }
}
Console.WriteLine($"   Done! {issueStats}");

// 7. Final statistics
Console.WriteLine("\n=== FINAL STATISTICS ===");
using (var cmd = new SqlCommand(@"
    SELECT
        (SELECT COUNT(*) FROM Zones) as Zones,
        (SELECT COUNT(*) FROM Users WHERE Role = 2) as Workers,
        (SELECT COUNT(*) FROM Users WHERE Role = 1) as Supervisors,
        (SELECT COUNT(*) FROM Attendances a JOIN Users u ON a.UserId = u.UserId
         WHERE u.Role = 2 AND CAST(a.CheckInEventTime AS DATE) = CAST(GETUTCDATE() AS DATE)) as WorkersPresent,
        (SELECT COUNT(*) FROM Attendances a JOIN Users u ON a.UserId = u.UserId
         WHERE u.Role = 1 AND CAST(a.CheckInEventTime AS DATE) = CAST(GETUTCDATE() AS DATE)) as SupervisorsPresent,
        (SELECT COUNT(*) FROM Tasks) as TotalTasks,
        (SELECT COUNT(*) FROM Tasks WHERE Status = 0) as Pending,
        (SELECT COUNT(*) FROM Tasks WHERE Status = 1) as InProgress,
        (SELECT COUNT(*) FROM Tasks WHERE Status IN (2, 4)) as Completed,
        (SELECT COUNT(*) FROM Tasks WHERE Status IN (2, 4) AND CAST(CompletedAt AS DATE) = CAST(GETUTCDATE() AS DATE)) as CompletedToday
", connection))
using (var reader = cmd.ExecuteReader())
{
    if (reader.Read())
    {
        Console.WriteLine($"Zones: {reader.GetInt32(0)}");
        Console.WriteLine($"Workers: {reader.GetInt32(1)} (Present: {reader.GetInt32(3)}, Absent: {reader.GetInt32(1) - reader.GetInt32(3)})");
        Console.WriteLine($"Supervisors: {reader.GetInt32(2)} (Present: {reader.GetInt32(4)}, Absent: {reader.GetInt32(2) - reader.GetInt32(4)})");
        Console.WriteLine($"Total Tasks: {reader.GetInt32(5)}");
        Console.WriteLine($"  - Pending: {reader.GetInt32(6)}");
        Console.WriteLine($"  - In Progress: {reader.GetInt32(7)}");
        Console.WriteLine($"  - Completed: {reader.GetInt32(8)} (Today: {reader.GetInt32(9)})");
    }
}

Console.WriteLine("\nAll done! Refresh your browser.");
