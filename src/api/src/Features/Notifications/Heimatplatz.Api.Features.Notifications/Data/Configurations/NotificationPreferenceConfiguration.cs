using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Notifications.Data.Configurations;

/// <summary>
/// Entity Framework configuration for NotificationPreference
/// </summary>
public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");

        builder.HasKey(np => np.Id);

        builder.Property(np => np.Location)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(np => np.IsEnabled)
            .IsRequired();

        // Relationship with User
        builder.HasOne(np => np.User)
            .WithMany()
            .HasForeignKey(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for efficient querying by location
        builder.HasIndex(np => np.Location);

        // Index for efficient querying by user
        builder.HasIndex(np => np.UserId);
    }
}
