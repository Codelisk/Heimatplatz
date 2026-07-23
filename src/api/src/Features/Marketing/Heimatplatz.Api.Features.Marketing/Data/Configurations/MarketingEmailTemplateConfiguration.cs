using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Marketing.Data.Configurations;

public class MarketingEmailTemplateConfiguration : IEntityTypeConfiguration<MarketingEmailTemplate>
{
    public void Configure(EntityTypeBuilder<MarketingEmailTemplate> builder)
    {
        builder.ToTable("MarketingEmailTemplates");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(120);
        builder.HasIndex(x => x.Name).IsUnique();

        builder.Property(x => x.Description).HasMaxLength(500);
        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);
        builder.Property(x => x.Body)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.IsActive).IsRequired();

        // Reihenfolge in der Vorlagen-Auswahl
        builder.HasIndex(x => x.DisplayOrder);
    }
}
