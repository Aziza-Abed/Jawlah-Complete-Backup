using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

// EF Core configuration for RefreshToken entity
public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(e => e.RefreshTokenId);

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.Token)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(e => e.ExpiresAt)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.ReplacedByToken)
            .HasMaxLength(500);

        builder.Property(e => e.DeviceId)
            .HasMaxLength(100);

        builder.Property(e => e.IpAddress)
            .HasMaxLength(50);

        // Indexes for efficient querying
        builder.HasIndex(e => e.Token)
            .IsUnique()
            .HasDatabaseName("IX_RefreshToken_Token");

        builder.HasIndex(e => new { e.UserId, e.RevokedAt, e.ExpiresAt })
            .HasDatabaseName("IX_RefreshToken_User_Active");

        builder.HasIndex(e => e.ExpiresAt)
            .HasDatabaseName("IX_RefreshToken_ExpiresAt");

        // Relationship with User
        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ignore computed properties
        builder.Ignore(e => e.IsExpired);
        builder.Ignore(e => e.IsRevoked);
        builder.Ignore(e => e.IsActive);
    }
}
