using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class MunicipalityConfiguration : IEntityTypeConfiguration<Municipality>
{
    public void Configure(EntityTypeBuilder<Municipality> builder)
    {
        builder.HasKey(e => e.MunicipalityId);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.NameEnglish)
            .HasMaxLength(200);

        builder.Property(e => e.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Region)
            .HasMaxLength(100);

        builder.Property(e => e.ContactEmail)
            .HasMaxLength(100);

        builder.Property(e => e.ContactPhone)
            .HasMaxLength(50);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.LogoUrl)
            .HasMaxLength(500);

        // Bounding box coordinates
        builder.Property(e => e.MinLatitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.MaxLatitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.MinLongitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.MaxLongitude)
            .HasPrecision(18, 15);

        // Work schedule defaults
        builder.Property(e => e.DefaultStartTime)
            .IsRequired();

        builder.Property(e => e.DefaultEndTime)
            .IsRequired();

        builder.Property(e => e.DefaultGraceMinutes)
            .IsRequired()
            .HasDefaultValue(15);

        builder.Property(e => e.MaxAcceptableAccuracyMeters)
            .HasDefaultValue(150.0);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Unique code index
        builder.HasIndex(e => e.Code)
            .IsUnique()
            .HasDatabaseName("IX_Municipality_Code_Unique");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Municipality_IsActive");

        // Navigation properties are configured via the child entity configurations
    }
}
