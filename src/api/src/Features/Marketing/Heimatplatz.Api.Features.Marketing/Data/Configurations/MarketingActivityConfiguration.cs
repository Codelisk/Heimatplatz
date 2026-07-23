using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Marketing.Data.Configurations;

public class MarketingActivityConfiguration : IEntityTypeConfiguration<MarketingActivity>
{
    public void Configure(EntityTypeBuilder<MarketingActivity> builder)
    {
        builder.ToTable("MarketingActivities");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(4000);
        builder.Property(x => x.OccurredAt).IsRequired();

        // Historie gehoert zum Kontakt - beim Loeschen des Kontakts geht sie mit
        builder.HasOne(x => x.Contact)
            .WithMany(x => x.Activities)
            .HasForeignKey(x => x.ContactId)
            .OnDelete(DeleteBehavior.Cascade);

        // Timeline-Abfrage im Kontakt-Detail
        builder.HasIndex(x => new { x.ContactId, x.OccurredAt });
    }
}
