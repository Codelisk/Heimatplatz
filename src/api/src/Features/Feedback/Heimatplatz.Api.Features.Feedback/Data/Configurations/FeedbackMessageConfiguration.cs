using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Feedback.Data.Configurations;

public class FeedbackMessageConfiguration : IEntityTypeConfiguration<FeedbackMessage>
{
    public void Configure(EntityTypeBuilder<FeedbackMessage> builder)
    {
        builder.ToTable("FeedbackMessages");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Body)
            .IsRequired()
            .HasColumnType("TEXT");

        builder.Property(x => x.Author).IsRequired();

        // Anfrage loeschen entfernt den kompletten Verlauf (DSGVO-freundlich)
        builder.HasOne(x => x.Ticket)
            .WithMany(x => x.Messages)
            .HasForeignKey(x => x.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        // Verlauf wird chronologisch pro Ticket geladen
        builder.HasIndex(x => new { x.TicketId, x.CreatedAt });
    }
}
