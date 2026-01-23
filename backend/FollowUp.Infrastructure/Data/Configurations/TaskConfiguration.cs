using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class TaskConfiguration : IEntityTypeConfiguration<Core.Entities.Task>
{
    public void Configure(EntityTypeBuilder<Core.Entities.Task> builder)
    {
        builder.HasKey(e => e.TaskId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Tasks)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Priority)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.LocationDescription)
            .HasMaxLength(500);

        builder.Property(e => e.CompletionNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.Latitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.Longitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.EventTime)
            .IsRequired();

        builder.Property(e => e.IsSynced)
            .IsRequired();

        builder.Property(e => e.SyncVersion)
            .IsRequired();

        // for handling concurrent updates
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

        builder.HasIndex(e => new { e.AssignedToUserId, e.Status })
            .HasDatabaseName("IX_Task_AssignedUser_Status");

        builder.HasIndex(e => e.Priority);

        builder.HasIndex(e => e.DueDate);

        builder.HasIndex(e => e.ZoneId);

        builder.HasOne(e => e.AssignedToUser)
            .WithMany(u => u.AssignedTasks)
            .HasForeignKey(e => e.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.AssignedByUser)
            .WithMany()
            .HasForeignKey(e => e.AssignedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Zone)
            .WithMany(z => z.Tasks)
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        // Team relationship for shared tasks
        builder.HasOne(e => e.Team)
            .WithMany(t => t.AssignedTasks)
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.IsTeamTask)
            .IsRequired()
            .HasDefaultValue(false);

        builder.HasIndex(e => e.TeamId)
            .HasDatabaseName("IX_Task_TeamId");

        builder.HasIndex(e => e.IsTeamTask)
            .HasDatabaseName("IX_Task_IsTeamTask");
    }
}
