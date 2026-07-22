using System.Text.Json;
using Heimatplatz.Api.Features.WkoCompanies.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.WkoCompanies.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer WkoCompany Entity
/// </summary>
public class WkoCompanyConfiguration : IEntityTypeConfiguration<WkoCompany>
{
    public void Configure(EntityTypeBuilder<WkoCompany> builder)
    {
        builder.ToTable("WkoCompanies");

        builder.HasKey(c => c.Id);

        // === Grunddaten ===
        builder.Property(c => c.Name)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(c => c.CategoryText)
            .HasMaxLength(500);

        // === Adressdaten ===
        builder.Property(c => c.Street)
            .HasMaxLength(300);

        builder.Property(c => c.PostalCode)
            .HasMaxLength(10);

        builder.Property(c => c.City)
            .HasMaxLength(100);

        // === Kontaktdaten ===
        builder.Property(c => c.Phones)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new List<string>());

        builder.Property(c => c.Email)
            .HasMaxLength(300);

        builder.Property(c => c.Website)
            .HasMaxLength(500);

        builder.Property(c => c.OpeningHoursText)
            .HasMaxLength(1000);

        // === Firmendaten laut Gewerbedatenbank ===
        builder.Property(c => c.CompanyRegisterNumber)
            .HasMaxLength(50);

        builder.Property(c => c.CompanyCourt)
            .HasMaxLength(300);

        builder.Property(c => c.Gln)
            .HasMaxLength(50);

        builder.Property(c => c.LegalForm)
            .HasMaxLength(100);

        builder.Property(c => c.Permits)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<WkoCompanyPermit>>(v, (JsonSerializerOptions?)null) ?? new List<WkoCompanyPermit>());

        // === Amtliche Firmenbuch-Daten ===
        builder.Property(c => c.Euid)
            .HasMaxLength(50);

        builder.Property(c => c.FirmenbuchManagingDirectors)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<FirmenbuchPerson>>(v, (JsonSerializerOptions?)null) ?? new List<FirmenbuchPerson>());

        // === Scraping-Daten ===
        builder.Property(c => c.DetailUrl)
            .IsRequired()
            .HasMaxLength(1000);

        builder.Property(c => c.SourceSearchTerm)
            .HasMaxLength(100);

        builder.Property(c => c.ContentHash)
            .HasMaxLength(64);

        builder.Property(c => c.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        // === Indizes fuer haeufige Abfragen ===
        builder.HasIndex(c => c.WkoFirmaId).IsUnique();
        builder.HasIndex(c => c.City);
        builder.HasIndex(c => c.PostalCode);
        builder.HasIndex(c => c.IsActive);
        builder.HasIndex(c => c.CreatedAt);
        builder.HasIndex(c => c.FoundedDate);
    }
}
