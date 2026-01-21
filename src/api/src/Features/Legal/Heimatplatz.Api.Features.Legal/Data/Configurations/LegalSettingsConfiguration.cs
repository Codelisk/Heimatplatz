using Heimatplatz.Api.Features.Legal.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Legal.Data.Configurations;

public class LegalSettingsConfiguration : IEntityTypeConfiguration<LegalSettings>
{
    public void Configure(EntityTypeBuilder<LegalSettings> builder)
    {
        builder.ToTable("LegalSettings");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SettingType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.ResponsiblePartyJson)
            .HasColumnType("TEXT");

        builder.Property(x => x.SectionsJson)
            .HasColumnType("TEXT");

        builder.Property(x => x.Version)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(x => x.EffectiveDate)
            .IsRequired();

        builder.Property(x => x.IsActive)
            .IsRequired()
            .HasDefaultValue(false);

        // Index fuer schnellen Zugriff auf aktive Version eines Typs
        builder.HasIndex(x => new { x.SettingType, x.IsActive });

        // Index fuer Versionshistorie
        builder.HasIndex(x => new { x.SettingType, x.EffectiveDate });
    }
}
