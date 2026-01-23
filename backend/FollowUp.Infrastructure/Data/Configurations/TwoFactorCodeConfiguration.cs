using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class TwoFactorCodeConfiguration : IEntityTypeConfiguration<TwoFactorCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorCode> builder)
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.CodeHash)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.IsUsed)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.Purpose)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.FailedAttempts)
            .IsRequired();

        builder.Property(e => e.PhoneNumber)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(e => e.DeviceId)
            .HasMaxLength(100);

        // Indexes for efficient querying
        builder.HasIndex(e => new { e.UserId, e.IsUsed, e.ExpiresAt })
            .HasDatabaseName("IX_TwoFactorCode_User_Active");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_TwoFactorCode_ExpiresAt");

        // Relationship with User
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
