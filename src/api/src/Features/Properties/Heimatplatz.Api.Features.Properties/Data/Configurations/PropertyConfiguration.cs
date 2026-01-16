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

        builder.Property(p => p.Titel)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Adresse)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Ort)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Plz)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(p => p.Preis)
            .HasPrecision(12, 2);

        builder.Property(p => p.AnbieterName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.Beschreibung)
            .HasMaxLength(4000);

        // JSON-Columns fuer Listen
        builder.Property(p => p.Ausstattung)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        builder.Property(p => p.BildUrls)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>()
            );

        // Foreign Key zu User (Verkaeufer)
        builder.Property(p => p.UserId)
            .IsRequired();

        builder.HasOne(p => p.User)
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indizes fuer haeufige Abfragen
        builder.HasIndex(p => p.Typ);
        builder.HasIndex(p => p.Ort);
        builder.HasIndex(p => p.Preis);
        builder.HasIndex(p => p.CreatedAt);
        builder.HasIndex(p => p.UserId);
    }
}
