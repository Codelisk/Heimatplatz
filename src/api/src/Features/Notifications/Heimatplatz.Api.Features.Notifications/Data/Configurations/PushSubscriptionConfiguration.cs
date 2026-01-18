using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Notifications.Data.Configurations;

/// <summary>
/// Entity Framework configuration for PushSubscription
/// </summary>
public class PushSubscriptionConfiguration : IEntityTypeConfiguration<PushSubscription>
{
    public void Configure(EntityTypeBuilder<PushSubscription> builder)
    {
        builder.ToTable("PushSubscriptions");

        builder.HasKey(ps => ps.Id);

        builder.Property(ps => ps.DeviceToken)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(ps => ps.Platform)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(ps => ps.SubscribedAt)
            .IsRequired();

        // Relationship with User
        builder.HasOne(ps => ps.User)
            .WithMany()
            .HasForeignKey(ps => ps.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for efficient querying by user
        builder.HasIndex(ps => ps.UserId);

        // Unique constraint on device token
        builder.HasIndex(ps => ps.DeviceToken)
            .IsUnique();
    }
}
