using Heimatplatz.Api.Features.Partners.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Partners.Data.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.ToTable("Partners");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Category)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.Description)
            .HasMaxLength(2000);

        builder.Property(x => x.WebsiteUrl)
            .HasMaxLength(500);

        builder.Property(x => x.LogoUrl)
            .HasMaxLength(500);

        builder.Property(x => x.Region)
            .HasMaxLength(200);

        builder.Property(x => x.SourceName)
            .HasMaxLength(200);

        builder.Property(x => x.SellerName)
            .HasMaxLength(200);

        builder.Property(x => x.IsVisible)
            .IsRequired();

        // Oeffentliche Liste: sichtbare Partner in Anzeige-Reihenfolge
        builder.HasIndex(x => new { x.IsVisible, x.DisplayOrder });
    }
}
