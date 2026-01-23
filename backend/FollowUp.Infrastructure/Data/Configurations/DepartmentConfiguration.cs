using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(e => e.DepartmentId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Departments)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.NameEnglish)
            .HasMaxLength(200);

        builder.Property(e => e.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(e => e.Description)
            .HasMaxLength(1000);

        builder.Property(e => e.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        // Unique code per municipality
        builder.HasIndex(e => new { e.MunicipalityId, e.Code })
            .IsUnique()
            .HasDatabaseName("IX_Department_Municipality_Code_Unique");

        builder.HasIndex(e => e.MunicipalityId)
            .HasDatabaseName("IX_Department_MunicipalityId");

        builder.HasIndex(e => e.IsActive)
            .HasDatabaseName("IX_Department_IsActive");
    }
}
