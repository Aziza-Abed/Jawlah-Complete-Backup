using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class TaskTemplateConfiguration : IEntityTypeConfiguration<TaskTemplate>
{
    public void Configure(EntityTypeBuilder<TaskTemplate> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Frequency)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.LocationDescription)
            .HasMaxLength(500);

        builder.Property(e => e.Priority)
            .HasConversion<int>();

        builder.Property(e => e.TaskType)
            .HasConversion<int?>();

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany()
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Zone relationship
        builder.HasOne(e => e.Zone)
            .WithMany()
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);

        // Default assigned user (optional — SetNull if user is deleted)
        builder.HasOne(e => e.DefaultAssignedTo)
            .WithMany()
            .HasForeignKey(e => e.DefaultAssignedToUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes
        builder.HasIndex(e => e.MunicipalityId)
            .HasDatabaseName("IX_TaskTemplate_MunicipalityId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_TaskTemplate_IsActive");

        builder.HasIndex(e => new { e.MunicipalityId, e.IsActive })
            .HasDatabaseName("IX_TaskTemplate_Municipality_Active");
    }
}
