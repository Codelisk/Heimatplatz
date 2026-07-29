using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Marketing.Data.Configurations;

public class MarketingContactConfiguration : IEntityTypeConfiguration<MarketingContact>
{
    public void Configure(EntityTypeBuilder<MarketingContact> builder)
    {
        builder.ToTable("MarketingContacts");
        builder.HasKey(x => x.Id);

        // Adresse wird von den Handlern normalisiert (lowercase/trim) gespeichert - der
        // Unique-Index verhindert Dubletten pro Kontakt. Gefiltert, weil Firmenpool-Kontakte
        // ohne Adresse entstehen und mehrere NULL-Werte erlaubt bleiben muessen.
        // Die Filter-Syntax ("Email" IS NOT NULL) gilt fuer Postgres und SQLite gleichermassen.
        builder.Property(x => x.Email).HasMaxLength(320);
        builder.HasIndex(x => x.Email)
            .IsUnique()
            .HasFilter("\"Email\" IS NOT NULL");

        builder.Property(x => x.Name).HasMaxLength(200);
        builder.Property(x => x.Title).HasMaxLength(50);
        builder.Property(x => x.FirstName).HasMaxLength(100);
        builder.Property(x => x.LastName).HasMaxLength(100);
        builder.Property(x => x.Salutation).IsRequired();
        builder.Property(x => x.Company).HasMaxLength(200);
        builder.Property(x => x.Phone).HasMaxLength(50);
        builder.Property(x => x.City).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(8000);
        builder.Property(x => x.Source).HasMaxLength(50);

        // Herkunft aus dem Firmenpool: macht die Uebernahme idempotent und blendet
        // uebernommene Firmen im Pool aus. Ebenfalls gefiltert eindeutig.
        builder.Property(x => x.FirmenbuchFnr).HasMaxLength(20);
        builder.HasIndex(x => x.FirmenbuchFnr)
            .IsUnique()
            .HasFilter("\"FirmenbuchFnr\" IS NOT NULL");

        builder.Property(x => x.ContactType).IsRequired();
        builder.Property(x => x.Status).IsRequired();

        // Listenfilter (Status/Typ) auf der Kontakte-Seite
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.ContactType);

        // Faellige Wiedervorlagen
        builder.HasIndex(x => x.NextFollowUpAt);
    }
}
