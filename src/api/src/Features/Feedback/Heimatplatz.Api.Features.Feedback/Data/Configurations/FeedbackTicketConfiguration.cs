using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Heimatplatz.Api.Features.Feedback.Data.Configurations;

public class FeedbackTicketConfiguration : IEntityTypeConfiguration<FeedbackTicket>
{
    public void Configure(EntityTypeBuilder<FeedbackTicket> builder)
    {
        builder.ToTable("FeedbackTickets");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Subject)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.AppVersion).HasMaxLength(50);

        builder.Property(x => x.Category).IsRequired();
        builder.Property(x => x.Status).IsRequired();
        builder.Property(x => x.Source).IsRequired();
        builder.Property(x => x.LastMessageAt).IsRequired();

        // "Meine Anfragen" des Nutzers
        builder.HasIndex(x => x.UserId);
        // Intern-Liste: Filter auf Status, Sortierung nach letzter Aktivitaet
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.LastMessageAt);
    }
}
