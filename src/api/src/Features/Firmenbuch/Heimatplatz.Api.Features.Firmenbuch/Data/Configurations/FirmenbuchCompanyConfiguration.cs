using Heimatplatz.Api.Features.Firmenbuch.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Firmenbuch.Data.Configurations;

public class FirmenbuchCompanyConfiguration : IEntityTypeConfiguration<FirmenbuchCompany>
{
    public void Configure(EntityTypeBuilder<FirmenbuchCompany> builder)
    {
        builder.ToTable("FirmenbuchCompanies");

        builder.Property(c => c.Fnr).IsRequired().HasMaxLength(20);
        builder.HasIndex(c => c.Fnr).IsUnique();

        builder.Property(c => c.Name).IsRequired().HasMaxLength(500);
        builder.Property(c => c.Status).HasMaxLength(50);
        builder.Property(c => c.Sitz).HasMaxLength(200);
        builder.Property(c => c.RechtsformCode).HasMaxLength(10);
        builder.Property(c => c.RechtsformText).HasMaxLength(200);
        builder.Property(c => c.Rechtseigenschaft).HasMaxLength(200);
        builder.Property(c => c.GerichtCode).HasMaxLength(10);
        builder.Property(c => c.GerichtText).HasMaxLength(200);
        builder.Property(c => c.SourceOrtNr).HasMaxLength(10);

        // Filter-/Suchpfade der Katalog-Abfrage
        builder.HasIndex(c => c.Sitz);
        builder.HasIndex(c => c.Status);
    }
}
