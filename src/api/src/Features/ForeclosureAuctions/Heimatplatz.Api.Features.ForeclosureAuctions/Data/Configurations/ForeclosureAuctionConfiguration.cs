using Heimatplatz.Api.Features.ForeclosureAuctions.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.ForeclosureAuctions.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer ForeclosureAuction Entity
/// </summary>
public class ForeclosureAuctionConfiguration : IEntityTypeConfiguration<ForeclosureAuction>
{
    public void Configure(EntityTypeBuilder<ForeclosureAuction> builder)
    {
        builder.ToTable("ForeclosureAuctions");

        builder.HasKey(fa => fa.Id);

        // === Versteigerungs-Grunddaten ===
        builder.Property(fa => fa.AuctionDate)
            .IsRequired();

        builder.Property(fa => fa.Category)
            .IsRequired();

        builder.Property(fa => fa.ObjectDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(fa => fa.Status)
            .HasMaxLength(50);

        // === Adressdaten ===
        builder.Property(fa => fa.Address)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fa => fa.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fa => fa.PostalCode)
            .IsRequired()
            .HasMaxLength(10);

        // === Grundbuch-Daten ===
        builder.Property(fa => fa.RegistrationNumber)
            .HasMaxLength(50);

        builder.Property(fa => fa.CadastralMunicipality)
            .HasMaxLength(100);

        builder.Property(fa => fa.PlotNumber)
            .HasMaxLength(50);

        builder.Property(fa => fa.SheetNumber)
            .HasMaxLength(50);

        // === Flaechendaten ===
        builder.Property(fa => fa.TotalArea)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.BuildingArea)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.GardenArea)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.PlotArea)
            .HasPrecision(12, 2);

        // === Immobilien-Details ===
        builder.Property(fa => fa.ZoningDesignation)
            .HasMaxLength(100);

        builder.Property(fa => fa.BuildingCondition)
            .HasMaxLength(200);

        // === Versteigerungs-Details ===
        builder.Property(fa => fa.EstimatedValue)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.MinimumBid)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.OwnershipShare)
            .HasMaxLength(20);

        // === Rechtliche Daten ===
        builder.Property(fa => fa.CaseNumber)
            .HasMaxLength(100);

        builder.Property(fa => fa.Court)
            .HasMaxLength(200);

        builder.Property(fa => fa.EdictUrl)
            .HasMaxLength(1000);

        builder.Property(fa => fa.Notes)
            .HasMaxLength(2000);

        // === Dokumente ===
        builder.Property(fa => fa.FloorPlanUrl)
            .HasMaxLength(1000);

        builder.Property(fa => fa.SitePlanUrl)
            .HasMaxLength(1000);

        builder.Property(fa => fa.LongAppraisalUrl)
            .HasMaxLength(1000);

        builder.Property(fa => fa.ShortAppraisalUrl)
            .HasMaxLength(1000);

        // === Indizes fuer haeufige Abfragen ===
        builder.HasIndex(fa => fa.AuctionDate);
        builder.HasIndex(fa => fa.Category);
        builder.HasIndex(fa => fa.City);
        builder.HasIndex(fa => fa.PostalCode);
        builder.HasIndex(fa => fa.Status);
        builder.HasIndex(fa => fa.RegistrationNumber);
        builder.HasIndex(fa => fa.CreatedAt);
    }
}
