using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Marketing.Data.Configurations;

public class MarketingContactEmailConfiguration : IEntityTypeConfiguration<MarketingContactEmail>
{
    public void Configure(EntityTypeBuilder<MarketingContactEmail> builder)
    {
        builder.ToTable("MarketingContactEmails");
        builder.HasKey(x => x.Id);

        // Wie MarketingContact.Email: von den Handlern normalisiert gespeichert, eindeutig
        // ueber alle Zusatzadressen. Dass eine Zusatzadresse nicht zugleich Versand-Adresse
        // eines (anderen) Kontakts ist, pruefen die Handler - ein tabellenuebergreifender
        // Index ist mit EF nicht abbildbar.
        builder.Property(x => x.Email)
            .IsRequired()
            .HasMaxLength(320);
        builder.HasIndex(x => x.Email).IsUnique();

        builder.Property(x => x.Source).HasMaxLength(50);

        // Kontakt loeschen entfernt auch dessen Zusatzadressen
        builder.HasOne(x => x.Contact)
            .WithMany(x => x.AdditionalEmails)
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.ContactId);
    }
}
