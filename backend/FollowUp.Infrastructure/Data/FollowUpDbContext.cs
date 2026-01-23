using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FollowUp.Infrastructure.Data;

public class FollowUpDbContext : DbContext
{
    public FollowUpDbContext(DbContextOptions<FollowUpDbContext> options) : base(options)
    {
    }

    public DbSet<Municipality> Municipalities { get; set; } = null!;
    public DbSet<Department> Departments { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<Core.Entities.Task> Tasks { get; set; } = null!;
    public DbSet<Issue> Issues { get; set; } = null!;
    public DbSet<Zone> Zones { get; set; } = null!;
    public DbSet<UserZone> UserZones { get; set; } = null!;
    public DbSet<Notification> Notifications { get; set; } = null!;
    public DbSet<LocationHistory> LocationHistories { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;
    public DbSet<AuditLog> AuditLogs { get; set; } = null!;
    public DbSet<Appeal> Appeals { get; set; } = null!;
    public DbSet<TaskTemplate> TaskTemplates { get; set; } = null!;
    public DbSet<GisFile> GisFiles { get; set; } = null!;
    public DbSet<TwoFactorCode> TwoFactorCodes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // this line tells EF to look for our configuration files (the ones in the Configurations folder)
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(FollowUpDbContext).Assembly);
    }
}
