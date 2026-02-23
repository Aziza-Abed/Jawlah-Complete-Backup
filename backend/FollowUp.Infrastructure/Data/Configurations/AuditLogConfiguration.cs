using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.HasKey(e => e.AuditLogId);

        builder.Property(e => e.Action)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Details)
            .HasMaxLength(1000);

        builder.Property(e => e.Username)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(45); // IPv6 max length

        builder.Property(e => e.UserAgent)
            .HasMaxLength(500);

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // index on CreatedAt for date-range queries and retention
        builder.HasIndex(e => e.CreatedAt)
            .HasDatabaseName("IX_AuditLogs_CreatedAt");

        // composite index for filtered queries (action + date)
        builder.HasIndex(e => new { e.Action, e.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Action_CreatedAt");

        // navigation to user
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
