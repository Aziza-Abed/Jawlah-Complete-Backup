using FollowUp.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FollowUp.Infrastructure.Data.Configurations;

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.HasKey(e => e.NotificationId);

        // Municipality relationship
        builder.HasOne(e => e.Municipality)
            .WithMany(m => m.Notifications)
            .HasForeignKey(e => e.MunicipalityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Message)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(e => e.Type)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(e => e.IsRead)
            .IsRequired();

        builder.Property(e => e.IsSent)
            .IsRequired();

        builder.Property(e => e.CreatedAt)
            .IsRequired();

        builder.Property(e => e.FcmMessageId)
            .HasMaxLength(200);

        builder.Property(e => e.PayloadJson)
            .HasMaxLength(2000);

        builder.HasIndex(e => new { e.UserId, e.IsRead })
            .HasDatabaseName("IX_Notification_User_IsRead");

        builder.HasIndex(e => e.Type);

        builder.HasIndex(e => e.CreatedAt);

        builder.HasIndex(e => e.IsSent);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
