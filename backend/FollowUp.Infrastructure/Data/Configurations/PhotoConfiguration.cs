using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaskEntity = FollowUp.Core.Entities.Task;

namespace FollowUp.Infrastructure.Data.Configurations;

public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
{
    public void Configure(EntityTypeBuilder<Photo> builder)
    {
        builder.HasKey(e => e.PhotoId);

        builder.Property(e => e.PhotoUrl)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.EntityType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.EntityId)
            .IsRequired();

        builder.Property(e => e.OrderIndex)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(e => e.UploadedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        builder.Property(e => e.CreatedAt)
            .IsRequired()
            .HasDefaultValueSql("GETUTCDATE()");

        // indexes for performance
        builder.HasIndex(e => new { e.EntityType, e.EntityId })
            .HasDatabaseName("IX_Photos_EntityType_EntityId");

        builder.HasIndex(e => e.UploadedAt)
            .HasDatabaseName("IX_Photos_UploadedAt");

        // navigation to user who uploaded (optional)
        builder.HasOne(e => e.UploadedByUser)
            .WithMany()
            .HasForeignKey(e => e.UploadedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        // navigation to Task (optional - photo can belong to task)
        builder.HasOne(e => e.Task)
            .WithMany(t => t.Photos)
            .HasForeignKey(e => e.TaskId)
            .OnDelete(DeleteBehavior.Cascade);

        // navigation to Issue (optional - photo can belong to issue)
        builder.HasOne(e => e.Issue)
            .WithMany(i => i.Photos)
            .HasForeignKey(e => e.IssueId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
