using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class TeamConfiguration : IEntityTypeConfiguration<Team>
{
    public void Configure(EntityTypeBuilder<Team> builder)
    {
        builder.HasKey(e => e.TeamId);

        // Department relationship
        builder.HasOne(e => e.Department)
            .WithMany(d => d.Teams)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Team leader relationship (optional)
        builder.HasOne(e => e.TeamLeader)
            .WithMany()
            .HasForeignKey(e => e.TeamLeaderId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.MaxMembers)
            .IsRequired()
            .HasDefaultValue(10);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Unique code per department
        builder.HasIndex(e => new { e.DepartmentId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_Team_Department_Code_Unique");

        builder.HasIndex(e => e.DepartmentId)
            .HasDatabaseName("IX_Team_DepartmentId");

        builder.HasIndex(e => e.TeamLeaderId)
            .HasDatabaseName("IX_Team_TeamLeaderId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Team_IsActive");
    }
}
