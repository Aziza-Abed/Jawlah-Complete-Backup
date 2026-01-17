using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jawlah.Infrastructure.Data;

public class JawlahDbContext : DbContext
{
    public JawlahDbContext(DbContextOptions<JawlahDbContext> options) : base(options)
    {
    }

    public DbSet<Municipality> Municipalities { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<Core.Entities.Task> Tasks { get; set; } = null!;
    public DbSet<Issue> Issues { get; set; } = null!;
    public DbSet<Zone> Zones { get; set; } = null!;
    public DbSet<UserZone> UserZones { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<RefreshToken> RefreshTokens { get; set; } = null!;
    public DbSet<LocationHistory> LocationHistories { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // this line tells EF to look for our configuration files (the ones in the Configurations folder)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(JawlahDbContext).Assembly);
    }
}
