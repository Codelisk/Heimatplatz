using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Marketing.Data.Configurations;

public class MarketingEmailConfiguration : IEntityTypeConfiguration<MarketingEmail>
{
    public void Configure(EntityTypeBuilder<MarketingEmail> builder)
    {
        builder.ToTable("MarketingEmails");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(x => x.Body)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.Keywords).HasMaxLength(2000);
        builder.Property(x => x.MessageId).HasMaxLength(500);
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.SentAt).IsRequired();

        // Kontakt loeschen entfernt auch dessen Versand-Historie (DSGVO-freundlich)
        builder.HasOne(x => x.Contact)
            .WithMany(x => x.Emails)
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // Reply-Zuordnung sucht ueber die Message-Id versendeter Mails
        builder.HasIndex(x => x.MessageId);
        builder.HasIndex(x => x.SentAt);
    }
}
