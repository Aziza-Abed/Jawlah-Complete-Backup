using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class LocationHistoryConfiguration : IEntityTypeConfiguration<LocationHistory>
{
    public void Configure(EntityTypeBuilder<LocationHistory> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Latitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(x => x.Longitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(x => x.Timestamp)
            .IsRequired();
            
        builder.HasOne(x => x.User)
            .WithMany()
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
            
        // index for faster queries
        builder.HasIndex(x => new { x.UserId, x.Timestamp })
            .HasDatabaseName("IX_LocationHistory_User_Timestamp");
    }
}
