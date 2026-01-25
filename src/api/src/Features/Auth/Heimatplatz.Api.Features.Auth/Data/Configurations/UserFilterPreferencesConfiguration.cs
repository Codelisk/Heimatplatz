using Heimatplatz.Api.Features.Auth.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Auth.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer UserFilterPreferences Entity
/// </summary>
public class UserFilterPreferencesConfiguration : IEntityTypeConfiguration<UserFilterPreferences>
{
    public void Configure(EntityTypeBuilder<UserFilterPreferences> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.UserId)
            .IsRequired();

        builder.Property(x => x.SelectedOrtesJson)
            .HasMaxLength(4000)
            .HasDefaultValue("[]");

        builder.Property(x => x.SelectedAgeFilter)
            .HasDefaultValue(0);

        builder.Property(x => x.IsHausSelected)
            .HasDefaultValue(true);

        builder.Property(x => x.IsGrundstueckSelected)
            .HasDefaultValue(true);

        builder.Property(x => x.IsZwangsversteigerungSelected)
            .HasDefaultValue(true);

        // Ein User kann nur eine FilterPreferences-Eintrag haben
        builder.HasIndex(x => x.UserId)
            .IsUnique();

        // Foreign Key zu User
        builder.HasOne(x => x.User)
            .WithOne()
            .HasForeignKey<UserFilterPreferences>(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
