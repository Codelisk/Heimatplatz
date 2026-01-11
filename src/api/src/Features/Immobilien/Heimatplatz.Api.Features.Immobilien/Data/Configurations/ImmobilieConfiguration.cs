using Heimatplatz.Api.Features.Immobilien.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Immobilien.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer Immobilie Entity
/// </summary>
public class ImmobilieConfiguration : IEntityTypeConfiguration<Immobilie>
{
    public void Configure(EntityTypeBuilder<Immobilie> builder)
    {
        builder.ToTable("Immobilien");

        // Primaerschluessel
        builder.HasKey(i => i.Id);

        // Indizes fuer haeufige Filter-Queries
        builder.HasIndex(i => i.Typ);
        builder.HasIndex(i => i.Status);
        builder.HasIndex(i => i.Preis);
        builder.HasIndex(i => i.Wohnflaeche);
        builder.HasIndex(i => i.Ort);
        builder.HasIndex(i => new { i.Ort, i.Bezirk });

        // String-Constraints
        builder.Property(i => i.Titel)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(i => i.Beschreibung)
            .HasMaxLength(4000);

        builder.Property(i => i.Ort)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Bezirk)
            .HasMaxLength(100);

        builder.Property(i => i.Region)
            .HasMaxLength(100);

        builder.Property(i => i.Land)
            .HasMaxLength(2)
            .HasDefaultValue("AT");

        builder.Property(i => i.Waehrung)
            .HasMaxLength(3)
            .HasDefaultValue("EUR");

        builder.Property(i => i.ZusatzInfo)
            .HasMaxLength(100);

        // Praezsision fuer Dezimalwerte
        builder.Property(i => i.Preis)
            .HasPrecision(18, 2);

        builder.Property(i => i.Wohnflaeche)
            .HasPrecision(18, 2);

        builder.Property(i => i.Grundstuecksflaeche)
            .HasPrecision(18, 2);

        // Beziehung zu Bildern
        builder.HasMany(i => i.Bilder)
            .WithOne(b => b.Immobilie)
            .HasForeignKey(b => b.ImmobilieId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
