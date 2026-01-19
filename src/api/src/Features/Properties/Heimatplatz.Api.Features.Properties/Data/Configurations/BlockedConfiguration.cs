using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Properties.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer Blocked Entity
/// </summary>
public class BlockedConfiguration : IEntityTypeConfiguration<Blocked>
{
    public void Configure(EntityTypeBuilder<Blocked> builder)
    {
        builder.ToTable("BlockedProperties");

        builder.HasKey(b => b.Id);

        // Foreign Key zu User (Kaeufer)
        builder.Property(b => b.UserId)
            .IsRequired();

        builder.HasOne(b => b.User)
            .WithMany()
            .HasForeignKey(b => b.UserId)
            .OnDelete(DeleteBehavior.Cascade); // Wenn User geloescht wird, auch Blockierungen loeschen

        // Foreign Key zu Property
        builder.Property(b => b.PropertyId)
            .IsRequired();

        builder.HasOne(b => b.Property)
            .WithMany()
            .HasForeignKey(b => b.PropertyId)
            .OnDelete(DeleteBehavior.Cascade); // Wenn Property geloescht wird, auch Blockierungen loeschen

        // Composite Unique Index: Ein User kann eine Property nur einmal blockieren
        builder.HasIndex(b => new { b.UserId, b.PropertyId })
            .IsUnique();

        // Index fuer haeufige Abfragen (alle blockierten Properties eines Users)
        builder.HasIndex(b => b.UserId);

        // Index fuer CreatedAt (falls wir nach Datum sortieren)
        builder.HasIndex(b => b.CreatedAt);
    }
}
