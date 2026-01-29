using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Properties.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer Property Entity
/// </summary>
public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Address)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.PostalCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.Price)
            .HasPrecision(12, 2);

        builder.Property(p => p.SellerName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Description)
            .HasMaxLength(4000);

        // JSON-Columns fuer Listen
        builder.Property(p => p.Features)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(p => p.ImageUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        // Typ-spezifische Daten als JSON
        builder.Property(p => p.TypeSpecificData)
            .HasColumnType("TEXT")
            .IsRequired();

        // Foreign Key zu User (Verkaeufer)
        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indizes fuer haeufige Abfragen
        builder.HasIndex(p => p.Type);
        builder.HasIndex(p => p.City);
        builder.HasIndex(p => p.Price);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.UserId);

        // Import-Tracking Felder
        builder.Property(p => p.SourceName)
            .HasMaxLength(100);

        builder.Property(p => p.SourceId)
            .HasMaxLength(200);

        builder.Property(p => p.SourceUrl)
            .HasMaxLength(2000);

        // Unique Index fuer Import-Duplikat-Erkennung (nur wenn beide Felder gesetzt sind)
        // SQLite verwendet andere Syntax als SQL Server
        builder.HasIndex(p => new { p.SourceName, p.SourceId })
            .IsUnique()
            .HasFilter("\"SourceName\" IS NOT NULL AND \"SourceId\" IS NOT NULL");
    }
}
