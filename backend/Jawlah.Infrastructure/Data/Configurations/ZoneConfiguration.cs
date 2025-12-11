using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class ZoneConfiguration : IEntityTypeConfiguration<Zone>
{
    public void Configure(EntityTypeBuilder<Zone> builder)
    {
        builder.HasKey(e => e.ZoneId);

        builder.Property(e => e.ZoneName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.ZoneCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.Boundary)
            .HasColumnType("geography");

        builder.Property(e => e.BoundaryGeoJson)
            .HasColumnType("nvarchar(max)");

        builder.Property(e => e.CenterLatitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(e => e.CenterLongitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(e => e.AreaSquareMeters)
            .IsRequired();

        builder.Property(e => e.District)
            .HasMaxLength(100);

        builder.Property(e => e.Version)
            .IsRequired();

        builder.Property(e => e.VersionDate)
            .IsRequired();

        builder.Property(e => e.VersionNotes)
            .HasMaxLength(500);

        builder.Property(e => e.IsActive)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.HasIndex(e => e.ZoneCode)
            .IsUnique()
            .HasDatabaseName("IX_Zone_ZoneCode_Unique");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Zone_IsActive");

        builder.HasIndex(e => e.District);
    }
}
