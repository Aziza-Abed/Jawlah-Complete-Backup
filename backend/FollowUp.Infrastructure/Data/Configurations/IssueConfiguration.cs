using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class IssueConfiguration : IEntityTypeConfiguration<Issue>
{
    public void Configure(EntityTypeBuilder<Issue> builder)
    {
        builder.HasKey(e => e.IssueId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Issues)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

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
            .HasColumnType("float");

        builder.Property(e => e.Longitude)
            .IsRequired()
            .HasColumnType("float");

        builder.Property(e => e.LocationDescription)
            .HasMaxLength(500);

        builder.Property(e => e.PhotoUrl)
            .HasMaxLength(500);

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

        // ClientId for idempotent sync (prevents duplicate issues on retry)
        builder.Property(e => e.ClientId)
            .HasMaxLength(36);

        // Unique index: same user cannot submit same ClientId twice
        builder.HasIndex(e => new { e.ReportedByUserId, e.ClientId })
            .HasDatabaseName("IX_Issue_UniqueClientId")
            .HasFilter("[ClientId] IS NOT NULL")
            .IsUnique();

        // for handling concurrent updates
        builder.Property(e => e.RowVersion)
            .IsRowVersion();

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

        // Issue forwarding to departments (SR15)
        builder.Property(e => e.ForwardingNotes).HasMaxLength(1000);
        builder.HasOne(e => e.ForwardedToDepartment)
            .WithMany()
            .HasForeignKey(e => e.ForwardedToDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
