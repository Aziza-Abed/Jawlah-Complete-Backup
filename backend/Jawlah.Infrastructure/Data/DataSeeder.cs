using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using NetTopologySuite.Geometries;
using Task = System.Threading.Tasks.Task;

namespace Jawlah.Infrastructure.Data;

public class DataSeeder
{
    private readonly JawlahDbContext _context;
    private readonly GeometryFactory _geometryFactory;
    private readonly IConfiguration _configuration;

    public DataSeeder(JawlahDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
    }

    public async Task SeedAsync()
    {
        // make sure the database is there
        await _context.Database.EnsureCreatedAsync();

        // add users if none exist
        if (!await _context.Users.AnyAsync())
        {
            await SeedUsersAsync();
        }

        // add zones if none exist
        if (!await _context.Zones.AnyAsync())
        {
            await SeedZonesAsync();
        }

        await _context.SaveChangesAsync();

        // assign workers to zones
        if (!await _context.UserZones.AnyAsync())
        {
            await SeedUserZonesAsync();
        }

        // add some sample tasks
        if (!await _context.Tasks.AnyAsync())
        {
            await SeedTasksAsync();
        }

        await _context.SaveChangesAsync();
    }

    private async Task SeedUsersAsync()
    {
        var adminUser = new User
        {
            Username = "admin",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_configuration["Seeding:AdminPassword"] ?? "Admin@123", workFactor: 12),
            Email = "admin@jawlah.com",
            PhoneNumber = "+970591234567",
            FullName = "System Administrator",
            Role = UserRole.Admin,
            WorkerType = null,
            Department = "IT",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        var supervisorUser = new User
        {
            Username = "supervisor",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_configuration["Seeding:SupervisorPassword"] ?? "Super@123", workFactor: 12),
            Email = "supervisor@jawlah.com",
            PhoneNumber = "+970592345678",
            FullName = "Field Supervisor",
            Role = UserRole.Supervisor,
            WorkerType = null,
            Department = "Operations",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        var worker1 = new User
        {
            Username = "worker1",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_configuration["Seeding:WorkerPassword"] ?? "Worker@123", workFactor: 12),
            Pin = _configuration["Seeding:Worker1Pin"] ?? "1234",
            Email = "worker1@jawlah.com",
            PhoneNumber = "+970593456789",
            FullName = "أحمد حسن",
            Role = UserRole.Worker,
            WorkerType = WorkerType.Sanitation,
            Department = "Field Operations",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        var worker2 = new User
        {
            Username = "worker2",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_configuration["Seeding:WorkerPassword"] ?? "Worker@123", workFactor: 12),
            Pin = _configuration["Seeding:Worker2Pin"] ?? "5678",
            Email = "worker2@jawlah.com",
            PhoneNumber = "+970594567890",
            FullName = "محمد علي",
            Role = UserRole.Worker,
            WorkerType = WorkerType.Inspector,
            Department = "Field Operations",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        var worker3 = new User
        {
            Username = "worker3",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(_configuration["Seeding:WorkerPassword"] ?? "Worker@123", workFactor: 12),
            Pin = _configuration["Seeding:Worker3Pin"] ?? "9012",
            Email = "worker3@jawlah.com",
            PhoneNumber = "+970595678901",
            FullName = "خالد محمود",
            Role = UserRole.Worker,
            WorkerType = WorkerType.Maintenance,
            Department = "Field Operations",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = null
        };

        await _context.Users.AddRangeAsync(adminUser, supervisorUser, worker1, worker2, worker3);
    }

    private async Task SeedZonesAsync()
    {
        // create polygon boundaries for GPS validation
        // central zone - area around Al-Bireh center
        var centralCoords = new Coordinate[]
        {
            new Coordinate(35.1984, 31.8988),
            new Coordinate(35.2084, 31.8988),
            new Coordinate(35.2084, 31.9088),
            new Coordinate(35.1984, 31.9088),
            new Coordinate(35.1984, 31.8988)
        };

        var zone1 = new Zone
        {
            ZoneName = "وسط البيرة",
            ZoneCode = "ZONE-001",
            Description = "Central Al-Bireh - المنطقة المركزية",
            Boundary = _geometryFactory.CreatePolygon(centralCoords),
            BoundaryGeoJson = null,
            CenterLatitude = 31.9038,
            CenterLongitude = 35.2034,
            AreaSquareMeters = 500000,
            District = "Central",
            Version = 1,
            VersionDate = DateTime.UtcNow,
            VersionNotes = "Initial zone setup",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // northern zone
        var northernCoords = new Coordinate[]
        {
            new Coordinate(35.1950, 31.9100),
            new Coordinate(35.2050, 31.9100),
            new Coordinate(35.2050, 31.9200),
            new Coordinate(35.1950, 31.9200),
            new Coordinate(35.1950, 31.9100)
        };

        var zone2 = new Zone
        {
            ZoneName = "شمال البيرة",
            ZoneCode = "ZONE-002",
            Description = "Northern Al-Bireh - المنطقة الشمالية",
            Boundary = _geometryFactory.CreatePolygon(northernCoords),
            BoundaryGeoJson = null,
            CenterLatitude = 31.9150,
            CenterLongitude = 35.2000,
            AreaSquareMeters = 750000,
            District = "Northern",
            Version = 1,
            VersionDate = DateTime.UtcNow,
            VersionNotes = "Initial zone setup",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        // southern zone
        var southernCoords = new Coordinate[]
        {
            new Coordinate(35.2000, 31.8870),
            new Coordinate(35.2100, 31.8870),
            new Coordinate(35.2100, 31.8970),
            new Coordinate(35.2000, 31.8970),
            new Coordinate(35.2000, 31.8870)
        };

        var zone3 = new Zone
        {
            ZoneName = "جنوب البيرة",
            ZoneCode = "ZONE-003",
            Description = "Southern Al-Bireh - المنطقة الجنوبية",
            Boundary = _geometryFactory.CreatePolygon(southernCoords),
            BoundaryGeoJson = null,
            CenterLatitude = 31.8920,
            CenterLongitude = 35.2050,
            AreaSquareMeters = 600000,
            District = "Southern",
            Version = 1,
            VersionDate = DateTime.UtcNow,
            VersionNotes = "Initial zone setup",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        await _context.Zones.AddRangeAsync(zone1, zone2, zone3);
    }

    private async Task SeedUserZonesAsync()
    {
        var workers = await _context.Users.Where(u => u.Role == UserRole.Worker).ToListAsync();
        var zones = await _context.Zones.ToListAsync();

        if (!workers.Any() || !zones.Any()) return;

        var userZones = new List<UserZone>();

        // assign worker1 to zone 1
        var worker1 = workers.FirstOrDefault(w => w.Pin == "1234");
        var zone1 = zones.FirstOrDefault(z => z.ZoneCode == "ZONE-001");
        if (worker1 != null && zone1 != null)
        {
            userZones.Add(new UserZone
            {
                UserId = worker1.UserId,
                ZoneId = zone1.ZoneId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        // assign worker2 to zone 2
        var worker2 = workers.FirstOrDefault(w => w.Pin == "5678");
        var zone2 = zones.FirstOrDefault(z => z.ZoneCode == "ZONE-002");
        if (worker2 != null && zone2 != null)
        {
            userZones.Add(new UserZone
            {
                UserId = worker2.UserId,
                ZoneId = zone2.ZoneId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        // assign worker3 to zone 3
        var worker3 = workers.FirstOrDefault(w => w.Pin == "9012");
        var zone3 = zones.FirstOrDefault(z => z.ZoneCode == "ZONE-003");
        if (worker3 != null && zone3 != null)
        {
            userZones.Add(new UserZone
            {
                UserId = worker3.UserId,
                ZoneId = zone3.ZoneId,
                AssignedAt = DateTime.UtcNow,
                IsActive = true
            });
        }

        await _context.UserZones.AddRangeAsync(userZones);
    }

    private async Task SeedTasksAsync()
    {
        var workers = await _context.Users.Where(u => u.Role == UserRole.Worker).ToListAsync();
        var zones = await _context.Zones.ToListAsync();
        var supervisor = await _context.Users.FirstOrDefaultAsync(u => u.Role == UserRole.Supervisor);

        if (!workers.Any() || !zones.Any() || supervisor == null) return;

        var worker1 = workers.FirstOrDefault(w => w.Pin == "1234");
        var zone1 = zones.FirstOrDefault(z => z.ZoneCode == "ZONE-001");

        if (worker1 == null || zone1 == null) return;

        var tasks = new List<Core.Entities.Task>
        {
            new Core.Entities.Task
            {
                Title = "تنظيف شارع القدس",
                Description = "تنظيف الشارع الرئيسي وإزالة المخلفات",
                AssignedToUserId = worker1.UserId,
                AssignedByUserId = supervisor.UserId,
                ZoneId = zone1.ZoneId,
                Priority = TaskPriority.High,
                Status = Core.Enums.TaskStatus.Pending,
                DueDate = DateTime.UtcNow.AddHours(6),
                Latitude = 31.9035,
                Longitude = 35.2052,
                LocationDescription = "شارع القدس - البلدة القديمة",
                CreatedAt = DateTime.UtcNow
            },
            new Core.Entities.Task
            {
                Title = "إزالة عوائق من الطريق",
                Description = "إزالة العوائق والحجارة من الطريق الفرعي",
                AssignedToUserId = worker1.UserId,
                AssignedByUserId = supervisor.UserId,
                ZoneId = zone1.ZoneId,
                Priority = TaskPriority.Medium,
                Status = Core.Enums.TaskStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(1),
                Latitude = 31.9041,
                Longitude = 35.2061,
                LocationDescription = "حي البيرة الجديدة",
                CreatedAt = DateTime.UtcNow
            },
            new Core.Entities.Task
            {
                Title = "صيانة إنارة الشوارع",
                Description = "فحص وإصلاح أعمدة الإنارة المعطلة",
                AssignedToUserId = worker1.UserId,
                AssignedByUserId = supervisor.UserId,
                ZoneId = zone1.ZoneId,
                Priority = TaskPriority.Low,
                Status = Core.Enums.TaskStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(2),
                Latitude = 31.9050,
                Longitude = 35.2070,
                LocationDescription = "شارع رام الله الرئيسي",
                CreatedAt = DateTime.UtcNow
            }
        };

        await _context.Tasks.AddRangeAsync(tasks);
    }
}
