using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.HasKey(e => e.IssueId);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .IsRequired()
            .HasMaxLength(2000);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Severity)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.Latitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(e => e.Longitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(e => e.LocationDescription)
            .HasMaxLength(500);

        builder.Property(e => e.PhotoUrl)
            .HasMaxLength(500);

        builder.Property(e => e.AdditionalPhotosJson)
            .HasMaxLength(2000);

        builder.Property(e => e.ReportedAt)
            .IsRequired();

        builder.Property(e => e.ResolutionNotes)
            .HasMaxLength(2000);

        builder.Property(e => e.EventTime)
            .IsRequired();

        builder.Property(e => e.IsSynced)
            .IsRequired();

        builder.Property(e => e.SyncVersion)
            .IsRequired();

        builder.HasIndex(e => new { e.ReportedByUserId, e.Status })
            .HasDatabaseName("IX_Issue_Reporter_Status");

        builder.HasIndex(e => e.Type);

        builder.HasIndex(e => e.Severity);

        builder.HasIndex(e => e.ZoneId);

        builder.HasIndex(e => e.ReportedAt);

        builder.HasOne(e => e.ReportedByUser)
            .WithMany(u => u.ReportedIssues)
            .HasForeignKey(e => e.ReportedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ResolvedByUser)
            .WithMany()
            .HasForeignKey(e => e.ResolvedByUserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Zone)
            .WithMany(z => z.Issues)
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
