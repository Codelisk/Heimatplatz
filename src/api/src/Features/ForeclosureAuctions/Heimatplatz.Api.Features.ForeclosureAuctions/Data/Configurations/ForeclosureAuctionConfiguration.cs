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

        builder.Property(fa => fa.AuctionDate)
            .IsRequired();

        builder.Property(fa => fa.Address)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(fa => fa.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(fa => fa.PostalCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(fa => fa.State)
            .IsRequired();

        builder.Property(fa => fa.Category)
            .IsRequired();

        builder.Property(fa => fa.ObjectDescription)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(fa => fa.EdictUrl)
            .HasMaxLength(1000);

        builder.Property(fa => fa.Notes)
            .HasMaxLength(2000);

        builder.Property(fa => fa.EstimatedValue)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.MinimumBid)
            .HasPrecision(12, 2);

        builder.Property(fa => fa.CaseNumber)
            .HasMaxLength(100);

        builder.Property(fa => fa.Court)
            .HasMaxLength(200);

        // Indizes fuer haeufige Abfragen
        builder.HasIndex(fa => fa.AuctionDate);
        builder.HasIndex(fa => fa.State);
        builder.HasIndex(fa => fa.Category);
        builder.HasIndex(fa => fa.City);
        builder.HasIndex(fa => fa.PostalCode);
        builder.HasIndex(fa => fa.CreatedAt);
    }
}
