using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class AppealConfiguration : IEntityTypeConfiguration<Appeal>
{
    public void Configure(EntityTypeBuilder<Appeal> builder)
    {
        builder.HasKey(e => e.AppealId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Appeals)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.AppealType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EntityId)
            .IsRequired();

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.WorkerExplanation)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.WorkerLatitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.WorkerLongitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.ExpectedLatitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.ExpectedLongitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.ReviewNotes)
            .HasMaxLength(1000);

        builder.Property(e => e.EvidencePhotoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.OriginalRejectionReason)
            .HasMaxLength(1000);

        builder.Property(e => e.SubmittedAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.IsSynced)
            .IsRequired();

        builder.Property(e => e.SyncVersion)
            .IsRequired();

        // Indexes for efficient querying
        builder.HasIndex(e => new { e.UserId, e.Status })
            .HasDatabaseName("IX_Appeal_User_Status");

        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_Appeal_Entity");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Appeal_Status");

        builder.HasIndex(e => e.SubmittedAt)
            .HasDatabaseName("IX_Appeal_SubmittedAt");

        // Relationships
        builder.HasOne(e => e.User)
            .WithMany(u => u.Appeals)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ReviewedByUser)
            .WithMany()
            .HasForeignKey(e => e.ReviewedByUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
