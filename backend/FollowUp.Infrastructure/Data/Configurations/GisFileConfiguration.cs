using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class GisFileConfiguration : IEntityTypeConfiguration<GisFile>
{
    public void Configure(EntityTypeBuilder<GisFile> builder)
    {
        builder.HasKey(e => e.GisFileId);

        builder.Property(e => e.FileType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.OriginalFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.StoredFileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(e => e.FileSize)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.FeaturesCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.Notes)
            .HasMaxLength(500);

        builder.Property(e => e.UploadedAt)
            .IsRequired();

        // Index for finding active file by type
        builder.HasIndex(e => new { e.FileType, e.IsActive })
            .HasDatabaseName("IX_GisFile_Type_Active");

        // Relationship to User
        builder.HasOne(e => e.UploadedBy)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
