using FollowUp.Core.Entities;
using FollowUp.Core.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(e => e.UserId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Users)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        // Supervisor-Worker relationship (self-referencing)
        builder.HasOne(e => e.Supervisor)
            .WithMany(s => s.SupervisedWorkers)
            .HasForeignKey(e => e.SupervisorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Username)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.PasswordHash)
            .IsRequired();

        builder.Property(e => e.FullName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Email)
            .HasMaxLength(100);

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.FcmToken)
            .HasMaxLength(255);

        // Department relationship (optional for admins)
        builder.HasOne(e => e.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(e => e.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Team relationship (optional for workers not in teams)
        builder.HasOne(e => e.Team)
            .WithMany(t => t.Members)
            .HasForeignKey(e => e.TeamId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Property(e => e.Role)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.WorkerType)
            .HasConversion<int?>();

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.Username).IsUnique();
        builder.HasIndex(e => e.Email);
    }
}
