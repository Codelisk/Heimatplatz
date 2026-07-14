using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Shiny.Extensions.Push;

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

        builder.Property(ps => ps.DeviceId)
            .HasMaxLength(100);

        builder.Property(ps => ps.AppId)
            .HasMaxLength(100);

        builder.Property(ps => ps.Environment)
            .HasConversion<string>()
            .HasMaxLength(20)
            .HasDefaultValue(PushEnvironment.Production)
            .IsRequired();

        builder.Property(ps => ps.TagsJson)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(ps => ps.TopicsJson)
            .IsRequired()
            .HasDefaultValue("[]");

        builder.Property(ps => ps.Locale)
            .HasMaxLength(35);

        builder.Property(ps => ps.AppVersion)
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

        // DeviceId is deliberately not unique: legacy clients may not provide one and
        // uniqueness semantics for nullable columns differ between supported databases.
        builder.HasIndex(ps => ps.DeviceId);

        // Unique constraint on device token
        builder.HasIndex(ps => ps.DeviceToken)
            .IsUnique();
    }
}
