using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;

namespace Heimatplatz.Api.Features.Feedback.Data.Entities;

/// <summary>
/// Eine Nachricht im Verlauf einer Feedback-Anfrage. Body darf leer sein,
/// wenn Anhaenge vorhanden sind (z.B. reine Sprachnachricht).
/// </summary>
public class FeedbackMessage : BaseEntity
{
    public Guid TicketId { get; set; }
    public FeedbackTicket Ticket { get; set; } = null!;

    public FeedbackAuthor Author { get; set; }

    public required string Body { get; set; }

    public ICollection<FeedbackAttachment> Attachments { get; set; } = [];
}
