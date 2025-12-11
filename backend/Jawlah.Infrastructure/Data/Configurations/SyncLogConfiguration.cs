using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> builder)
    {
        builder.HasKey(e => e.SyncLogId);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.EntityId)
            .IsRequired();

        builder.Property(e => e.Action)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.EventTime)
            .IsRequired();

        builder.Property(e => e.SyncTime)
            .IsRequired();

        builder.Property(e => e.HadConflict)
            .IsRequired();

        builder.Property(e => e.ConflictResolution)
            .HasMaxLength(50);

        builder.Property(e => e.ConflictDetails)
            .HasMaxLength(2000);

        builder.Property(e => e.DeviceId)
            .HasMaxLength(100);

        builder.Property(e => e.AppVersion)
            .HasMaxLength(20);

        builder.HasIndex(e => new { e.UserId, e.SyncTime })
            .HasDatabaseName("IX_SyncLog_User_SyncTime");

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_SyncLog_Entity");

        builder.HasIndex(e => e.HadConflict);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
