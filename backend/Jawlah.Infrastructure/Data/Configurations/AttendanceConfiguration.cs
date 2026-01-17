using Jawlah.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Jawlah.Infrastructure.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.HasKey(e => e.AttendanceId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Attendances)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.UserId)
            .IsRequired();

        builder.Property(e => e.CheckInEventTime)
            .IsRequired();

        builder.Property(e => e.CheckInLatitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(e => e.CheckInLongitude)
            .IsRequired()
            .HasPrecision(18, 15);

        builder.Property(e => e.CheckOutLatitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.CheckOutLongitude)
            .HasPrecision(18, 15);

        builder.Property(e => e.IsValidated)
            .IsRequired();

        builder.Property(e => e.ValidationMessage)
            .HasMaxLength(500);

        builder.Property(e => e.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsSynced)
            .IsRequired();

        builder.Property(e => e.SyncVersion)
            .IsRequired();

        builder.HasIndex(e => new { e.UserId, e.CheckInEventTime })
            .HasDatabaseName("IX_Attendance_User_CheckIn");

        builder.HasIndex(e => e.Status)
            .HasDatabaseName("IX_Attendance_Status");

        builder.HasIndex(e => e.ZoneId)
            .HasDatabaseName("IX_Attendance_ZoneId");

        builder.HasOne(e => e.User)
            .WithMany(u => u.AttendanceRecords)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Zone)
            .WithMany(z => z.AttendanceRecords)
            .HasForeignKey(e => e.ZoneId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
