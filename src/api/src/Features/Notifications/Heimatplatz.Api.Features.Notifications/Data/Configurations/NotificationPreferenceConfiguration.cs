using Heimatplatz.Api.Features.Notifications.Contracts;
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

        builder.Property(np => np.FilterMode)
            .IsRequired()
            .HasDefaultValue(NotificationFilterMode.All);

        builder.Property(np => np.IsEnabled)
            .IsRequired();

        builder.Property(np => np.SelectedLocationsJson)
            .HasMaxLength(4000)
            .HasDefaultValue("[]");

        builder.Property(np => np.IsHausSelected)
            .HasDefaultValue(true);

        builder.Property(np => np.IsGrundstueckSelected)
            .HasDefaultValue(true);

        builder.Property(np => np.IsZwangsversteigerungSelected)
            .HasDefaultValue(true);

        builder.Property(np => np.IsPrivateSelected)
            .HasDefaultValue(true);

        builder.Property(np => np.IsBrokerSelected)
            .HasDefaultValue(true);

        builder.Property(np => np.IsPortalSelected)
            .HasDefaultValue(true);

        builder.Property(np => np.ExcludedSellerSourceIdsJson)
            .HasMaxLength(4000)
            .HasDefaultValue("[]");

        // Relationship with User
        builder.HasOne(np => np.User)
            .WithMany()
            .HasForeignKey(np => np.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique index: one preference per user
        builder.HasIndex(np => np.UserId)
            .IsUnique();
    }
}
