using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial data using Entity Framework (proper UTF-8 encoding)
/// </summary>
public class DatabaseSeeder
{
    private readonly FollowUpDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DatabaseSeeder(FollowUpDbContext context, IPasswordHasher<User> passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async System.Threading.Tasks.Task SeedAsync()
    {
        // Check if already seeded
        if (await _context.Users.AnyAsync())
        {
            return; // Already has data
        }

        // 1. Municipality (Default - configure in appsettings.json for specific deployment)
        var municipality = new Municipality
        {
            Code = "DEFAULT",
            Name = "FollowUp",
            NameEnglish = "FollowUp System",
            CreatedAt = DateTime.UtcNow
        };
        _context.Municipalities.Add(municipality);
        await _context.SaveChangesAsync();

        // 2. Departments
        var servicesDept = new Department
        {
            MunicipalityId = municipality.MunicipalityId,
            Name = "قسم الخدمات",
            NameEnglish = "Services Department",
            Code = "SERVICES",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(servicesDept);

        var sanitationDept = new Department
        {
            MunicipalityId = municipality.MunicipalityId,
            Name = "قسم النظافة",
            NameEnglish = "Sanitation Department",
            Code = "SANITATION",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(sanitationDept);

        var maintenanceDept = new Department
        {
            MunicipalityId = municipality.MunicipalityId,
            Name = "قسم الصيانة",
            NameEnglish = "Maintenance Department",
            Code = "MAINTENANCE",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(maintenanceDept);

        var agricultureDept = new Department
        {
            MunicipalityId = municipality.MunicipalityId,
            Name = "قسم الزراعة",
            NameEnglish = "Agriculture Department",
            Code = "AGRICULTURE",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(agricultureDept);

        var publicWorksDept = new Department
        {
            MunicipalityId = municipality.MunicipalityId,
            Name = "قسم الأشغال",
            NameEnglish = "Public Works Department",
            Code = "PUBLICWORKS",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Departments.Add(publicWorksDept);

        await _context.SaveChangesAsync();

        // 3. Teams (example teams for sanitation and maintenance)
        var sanitationTeam1 = new Team
        {
            DepartmentId = sanitationDept.DepartmentId,
            Name = "فريق النظافة 1",
            Code = "SAN-T1",
            MaxMembers = 5,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Teams.Add(sanitationTeam1);

        var maintenanceTeam1 = new Team
        {
            DepartmentId = maintenanceDept.DepartmentId,
            Name = "فريق الصيانة 1",
            Code = "MAINT-T1",
            MaxMembers = 4,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        _context.Teams.Add(maintenanceTeam1);

        await _context.SaveChangesAsync();

        // 4. Users
        var admin = new User
        {
            Username = "admin",
            FullName = "مدير النظام",
            Email = "admin@albireh.ps",
            PhoneNumber = "+970599000000",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            MunicipalityId = municipality.MunicipalityId,
            DepartmentId = null, // Admin doesn't belong to a specific department
            TeamId = null,
            CreatedAt = DateTime.UtcNow
        };
        admin.PasswordHash = _passwordHasher.HashPassword(admin, "Admin@123");
        _context.Users.Add(admin);

        var supervisor = new User
        {
            Username = "supervisor",
            FullName = "أحمد المشرف",
            Email = "ahmad.supervisor@albireh.ps",
            PhoneNumber = "+970599000003",
            Role = UserRole.Supervisor,
            WorkerType = null, // Supervisors don't have a worker type
            Status = UserStatus.Active,
            MunicipalityId = municipality.MunicipalityId,
            DepartmentId = servicesDept.DepartmentId, // Supervisor of services department
            TeamId = null,
            ExpectedStartTime = new TimeSpan(7, 0, 0),
            ExpectedEndTime = new TimeSpan(15, 0, 0),
            CreatedAt = DateTime.UtcNow
        };
        supervisor.PasswordHash = _passwordHasher.HashPassword(supervisor, "Supervisor@123");
        _context.Users.Add(supervisor);

        var workers = new List<User>
        {
            new User
            {
                Username = "ahmad.hassan",
                FullName = "أحمد حسن محمود",
                Email = "ahmad.hassan@albireh.ps",
                PhoneNumber = "+970599000001",
                Role = UserRole.Worker,
                WorkerType = WorkerType.Sanitation,
                Status = UserStatus.Active,
                MunicipalityId = municipality.MunicipalityId,
                DepartmentId = sanitationDept.DepartmentId,
                TeamId = sanitationTeam1.TeamId, // Part of sanitation team 1
                ExpectedStartTime = new TimeSpan(6, 0, 0),
                ExpectedEndTime = new TimeSpan(14, 0, 0),
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "mohammed.ali",
                FullName = "محمد علي يوسف",
                Email = "mohammad.ali@albireh.ps",
                PhoneNumber = "+970599000002",
                Role = UserRole.Worker,
                WorkerType = WorkerType.Maintenance,
                Status = UserStatus.Active,
                MunicipalityId = municipality.MunicipalityId,
                DepartmentId = maintenanceDept.DepartmentId,
                TeamId = maintenanceTeam1.TeamId, // Part of maintenance team 1
                ExpectedStartTime = new TimeSpan(6, 0, 0),
                ExpectedEndTime = new TimeSpan(14, 0, 0),
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "fatima.ali",
                FullName = "فاطمة علي خالد",
                Email = "fatima.ali@albireh.ps",
                PhoneNumber = "+970599000004",
                Role = UserRole.Worker,
                WorkerType = WorkerType.Agriculture,
                Status = UserStatus.Active,
                MunicipalityId = municipality.MunicipalityId,
                DepartmentId = agricultureDept.DepartmentId,
                TeamId = null, // Agriculture worker
                ExpectedStartTime = new TimeSpan(7, 0, 0),
                ExpectedEndTime = new TimeSpan(15, 0, 0),
                CreatedAt = DateTime.UtcNow
            }
        };

        foreach (var worker in workers)
        {
            worker.PasswordHash = _passwordHasher.HashPassword(worker, "Worker@123");
            _context.Users.Add(worker);
        }

        await _context.SaveChangesAsync();

        Console.WriteLine("✅ Database seeded successfully with proper UTF-8 encoding!");
    }
}
