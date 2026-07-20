using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Marketing.Data.Configurations;

public class MarketingInboundEmailConfiguration : IEntityTypeConfiguration<MarketingInboundEmail>
{
    public void Configure(EntityTypeBuilder<MarketingInboundEmail> builder)
    {
        builder.ToTable("MarketingInboundEmails");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.FromAddress)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(x => x.FromName).HasMaxLength(200);
        builder.Property(x => x.Subject).HasMaxLength(500);
        builder.Property(x => x.BodyText).HasColumnType("TEXT");
        builder.Property(x => x.InReplyTo).HasMaxLength(500);
        builder.Property(x => x.ReceivedAt).IsRequired();

        // Idempotenz des Posteingang-Syncs: jede Mail nur einmal uebernehmen
        builder.Property(x => x.MessageId)
            .IsRequired()
            .HasMaxLength(500);
        builder.HasIndex(x => x.MessageId).IsUnique();

        // Kontakt loeschen entfernt auch dessen Rueckmeldungen (DSGVO-freundlich)
        builder.HasOne(x => x.Contact)
            .WithMany(x => x.InboundEmails)
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // Die referenzierte Versand-Mail haengt am selben Kontakt (gleiche Kaskade);
        // SetNull deckt den Fall ab, dass nur die Mail-Zeile entfernt wird.
        builder.HasOne(x => x.RepliedToEmail)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.MarketingEmailId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(x => x.ReceivedAt);
        builder.HasIndex(x => x.IsRead);
    }
}
