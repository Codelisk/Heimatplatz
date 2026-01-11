using Heimatplatz.Api.Features.Immobilien.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Immobilien.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer ImmobilieBild Entity
/// </summary>
public class ImmobilieBildConfiguration : IEntityTypeConfiguration<ImmobilieBild>
{
    public void Configure(EntityTypeBuilder<ImmobilieBild> builder)
    {
        builder.ToTable("ImmobilieBilder");

        // Primaerschluessel
        builder.HasKey(b => b.Id);

        // Indizes
        builder.HasIndex(b => b.ImmobilieId);
        builder.HasIndex(b => b.IstHauptbild);
        builder.HasIndex(b => new { b.ImmobilieId, b.Reihenfolge });

        // String-Constraints
        builder.Property(b => b.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(b => b.AltText)
            .HasMaxLength(200);

        // Defaults
        builder.Property(b => b.Reihenfolge)
            .HasDefaultValue(0);

        builder.Property(b => b.IstHauptbild)
            .HasDefaultValue(false);
    }
}
