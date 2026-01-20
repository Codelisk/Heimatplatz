using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Properties.Data.Configurations;

/// <summary>
/// EF Core Konfiguration fuer PropertyContactInfo Entity
/// </summary>
public class PropertyContactInfoConfiguration : IEntityTypeConfiguration<PropertyContactInfo>
{
    public void Configure(EntityTypeBuilder<PropertyContactInfo> builder)
    {
        builder.ToTable("PropertyContactInfos");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(200);

        builder.Property(c => c.Email)
            .HasMaxLength(254);

        builder.Property(c => c.Phone)
            .HasMaxLength(50);

        builder.Property(c => c.OriginalListingUrl)
            .HasMaxLength(2000);

        builder.Property(c => c.SourceName)
            .HasMaxLength(100);

        builder.Property(c => c.SourceId)
            .HasMaxLength(200);

        // Foreign Key zu Property
        builder.HasOne(c => c.Property)
            .WithMany(p => p.Contacts)
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indizes
        builder.HasIndex(c => c.PropertyId);
        builder.HasIndex(c => c.Type);
        builder.HasIndex(c => new { c.PropertyId, c.DisplayOrder });
    }
}
