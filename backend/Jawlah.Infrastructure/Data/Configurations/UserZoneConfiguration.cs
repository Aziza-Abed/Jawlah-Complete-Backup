using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class UserZoneConfiguration : IEntityTypeConfiguration<UserZone>
{
    public void Configure(EntityTypeBuilder<UserZone> builder)
    {
        builder.HasKey(e => new { e.UserId, e.ZoneId });

        builder.Property(e => e.AssignedAt)
            .IsRequired();

        builder.Property(e => e.AssignedByUserId)
            .IsRequired();

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.HasIndex(e => e.UserId)
            .HasDatabaseName("IX_UserZone_UserId");

        builder.HasIndex(e => e.ZoneId)
            .HasDatabaseName("IX_UserZone_ZoneId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_UserZone_IsActive");

        builder.HasOne(e => e.User)
            .WithMany(u => u.AssignedZones)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Zone)
            .WithMany(z => z.AssignedUsers)
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
