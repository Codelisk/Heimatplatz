using Heimatplatz.Api.Features.SrealListings.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.SrealListings.Data.Configurations;

public class SrealListingConfiguration : IEntityTypeConfiguration<SrealListing>
{
    public void Configure(EntityTypeBuilder<SrealListing> builder)
    {
        builder.ToTable("SrealListings");

        builder.HasKey(s => s.Id);

        // === Identifikation ===
        builder.Property(s => s.ExternalId)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.Title)
            .IsRequired()
            .HasMaxLength(500);

        // === Adressdaten ===
        builder.Property(s => s.Address)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(s => s.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.PostalCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(s => s.District)
            .HasMaxLength(100);

        builder.Property(s => s.State);

        // === Objektdaten ===
        builder.Property(s => s.ObjectType);

        builder.Property(s => s.BuyingType)
            .HasMaxLength(10);

        builder.Property(s => s.Price)
            .HasPrecision(12, 2);

        builder.Property(s => s.PriceText)
            .HasMaxLength(100);

        builder.Property(s => s.Commission)
            .HasMaxLength(200);

        builder.Property(s => s.LivingArea)
            .HasPrecision(12, 2);

        builder.Property(s => s.PlotArea)
            .HasPrecision(12, 2);

        builder.Property(s => s.Description)
            .HasMaxLength(10000);

        // === Energieausweis ===
        builder.Property(s => s.EnergyClass)
            .HasMaxLength(5);

        builder.Property(s => s.EnergyValue)
            .HasMaxLength(50);

        builder.Property(s => s.FGee)
            .HasMaxLength(20);

        builder.Property(s => s.FGeeClass)
            .HasMaxLength(5);

        // === Bilder (JSON) ===
        builder.Property(s => s.ImageUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        // === Quell-URL ===
        builder.Property(s => s.SourceUrl)
            .IsRequired()
            .HasMaxLength(1000);

        // === Makler-Kontakt ===
        builder.Property(s => s.AgentName)
            .HasMaxLength(200);

        builder.Property(s => s.AgentPhone)
            .HasMaxLength(50);

        builder.Property(s => s.AgentEmail)
            .HasMaxLength(200);

        builder.Property(s => s.AgentOffice)
            .HasMaxLength(200);

        // === Zusatzdaten ===
        builder.Property(s => s.Infrastructure)
            .HasMaxLength(4000);

        // === Scraping-Metadaten ===
        builder.Property(s => s.ContentHash)
            .HasMaxLength(64);

        builder.Property(s => s.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // === Navigation Properties ===
        builder.HasMany(s => s.Changes)
            .WithOne(c => c.SrealListing)
            .HasForeignKey(c => c.SrealListingId)
            .OnDelete(DeleteBehavior.Cascade);

        // === Indizes ===
        builder.HasIndex(s => s.ExternalId).IsUnique();
        builder.HasIndex(s => s.City);
        builder.HasIndex(s => s.PostalCode);
        builder.HasIndex(s => s.ObjectType);
        builder.HasIndex(s => s.Price);
        builder.HasIndex(s => s.IsActive);
        builder.HasIndex(s => s.State);
        builder.HasIndex(s => s.CreatedAt);
    }
}
