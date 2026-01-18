using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Properties.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer Favorite Entity
/// </summary>
public class FavoriteConfiguration : IEntityTypeConfiguration<Favorite>
{
    public void Configure(EntityTypeBuilder<Favorite> builder)
    {
        builder.ToTable("Favorites");

        builder.HasKey(f => f.Id);

        // Foreign Key zu User (Kaeufer)
        builder.Property(f => f.UserId)
            .IsRequired();

        builder.HasOne(f => f.User)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Wenn User geloescht wird, auch Favoriten loeschen

        // Foreign Key zu Property
        builder.Property(f => f.PropertyId)
            .IsRequired();

        builder.HasOne(f => f.Property)
            .WithMany()
            .HasForeignKey(f => f.PropertyId)
            .OnDelete(DeleteBehavior.Cascade); // Wenn Property geloescht wird, auch Favoriten loeschen

        // Composite Unique Index: Ein User kann eine Property nur einmal favorisieren
        builder.HasIndex(f => new { f.UserId, f.PropertyId })
            .IsUnique();

        // Index fuer haeufige Abfragen (alle Favoriten eines Users)
        builder.HasIndex(f => f.UserId);

        // Index fuer CreatedAt (falls wir nach Datum sortieren)
        builder.HasIndex(f => f.CreatedAt);
    }
}
