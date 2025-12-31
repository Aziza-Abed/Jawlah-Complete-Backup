using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Jawlah.Core.Entities;

namespace Jawlah.Infrastructure.Data.Configurations;

// these are special database settings to make the app faster when searching for data
public class AttendanceIndexConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        // index on UserId for quick user attendance lookups
        builder.HasIndex(a => a.UserId);

        // index on CheckInEventTime for date range queries
        builder.HasIndex(a => a.CheckInEventTime);

        // composite index for user + date queries (most common)
        builder.HasIndex(a => new { a.UserId, a.CheckInEventTime });

        // index on ZoneId for zone-based queries
        builder.HasIndex(a => a.ZoneId);
    }
}

public class TaskIndexConfiguration : IEntityTypeConfiguration<Jawlah.Core.Entities.Task>
{
    public void Configure(EntityTypeBuilder<Jawlah.Core.Entities.Task> builder)
    {
        // index on AssignedToUserId for user task queries
        builder.HasIndex(t => t.AssignedToUserId);

        // index on Status for filtering
        builder.HasIndex(t => t.Status);

        // index on DueDate for overdue queries
        builder.HasIndex(t => t.DueDate);

        // composite index for user + status queries
        builder.HasIndex(t => new { t.AssignedToUserId, t.Status });

        // index on SyncTime for sync operations
        builder.HasIndex(t => t.SyncTime);
    }
}

public class IssueIndexConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        // index on ReportedByUserId for user issue queries
        builder.HasIndex(i => i.ReportedByUserId);

        // index on Status for filtering
        builder.HasIndex(i => i.Status);

        // index on Severity for priority queries
        builder.HasIndex(i => i.Severity);

        // index on ReportedAt for date sorting
        builder.HasIndex(i => i.ReportedAt);
    }
}

public class UserIndexConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // index on Role for role-based queries
        builder.HasIndex(u => u.Role);

        // index on Status for active user queries
        builder.HasIndex(u => u.Status);

        // composite index for role + status (common query)
        builder.HasIndex(u => new { u.Role, u.Status });
    }
}
