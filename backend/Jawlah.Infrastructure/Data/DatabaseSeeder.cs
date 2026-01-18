using Jawlah.Core.Entities;
using Jawlah.Core.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Data;

/// <summary>
/// Seeds the database with initial data using Entity Framework (proper UTF-8 encoding)
/// </summary>
public class DatabaseSeeder
{
    private readonly JawlahDbContext _context;
    private readonly IPasswordHasher<User> _passwordHasher;

    public DatabaseSeeder(JawlahDbContext context, IPasswordHasher<User> passwordHasher)
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

        // 1. Municipality
        var municipality = new Municipality
        {
            Code = "BIREH",
            Name = "بلدية البيرة",
            NameEnglish = "Al-Bireh Municipality",
            CreatedAt = DateTime.UtcNow
        };
        _context.Municipalities.Add(municipality);
        await _context.SaveChangesAsync();

        // 2. Users
        var admin = new User
        {
            Username = "admin",
            FullName = "مدير النظام",
            Email = "admin@albireh.ps",
            PhoneNumber = "+970599000000",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
            MunicipalityId = municipality.MunicipalityId,
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
            Department = "قسم الخدمات",
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
                Pin = "1234",
                Email = "ahmad.hassan@albireh.ps",
                PhoneNumber = "+970599000001",
                Role = UserRole.Worker,
                WorkerType = WorkerType.Sanitation,
                Status = UserStatus.Active,
                MunicipalityId = municipality.MunicipalityId,
                Department = "قسم النظافة",
                ExpectedStartTime = new TimeSpan(6, 0, 0),
                ExpectedEndTime = new TimeSpan(14, 0, 0),
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "mohammed.ali",
                FullName = "محمد علي يوسف",
                Pin = "1235",
                Email = "mohammad.ali@albireh.ps",
                PhoneNumber = "+970599000002",
                Role = UserRole.Worker,
                WorkerType = WorkerType.Maintenance,
                Status = UserStatus.Active,
                MunicipalityId = municipality.MunicipalityId,
                Department = "قسم الصيانة",
                ExpectedStartTime = new TimeSpan(6, 0, 0),
                ExpectedEndTime = new TimeSpan(14, 0, 0),
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Username = "fatima.ali",
                FullName = "فاطمة علي خالد",
                Pin = "1236",
                Email = "fatima.ali@albireh.ps",
                PhoneNumber = "+970599000004",
                Role = UserRole.Worker,
                WorkerType = WorkerType.Inspector,
                Status = UserStatus.Active,
                MunicipalityId = municipality.MunicipalityId,
                Department = "قسم التفتيش",
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
