using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;

namespace Heimatplatz.Api.Features.Feedback.Data.Entities;

/// <summary>
/// Eine Feedback-Anfrage eines Nutzers (Wunsch, Problem, Frage, Lob) mit
/// Nachrichten-Verlauf. Die Unread-Flags steuern die Badges auf beiden Seiten:
/// HasUnreadForTeam = neue Nutzer-Nachricht wartet im Intern-Bereich,
/// HasUnreadForUser = Team-Antwort wurde vom Nutzer noch nicht geoeffnet.
/// </summary>
public class FeedbackTicket : BaseEntity
{
    public Guid UserId { get; set; }

    public FeedbackCategory Category { get; set; }

    /// <summary>Kurzbetreff; wird serverseitig aus der ersten Nachricht abgeleitet, wenn leer</summary>
    public required string Subject { get; set; }

    public FeedbackTicketStatus Status { get; set; }

    /// <summary>Plattform der Erstellung (Diagnose-Kontext bei Problem-Meldungen)</summary>
    public FeedbackSource Source { get; set; }

    /// <summary>App-Version bei Erstellung (nur App-Clients)</summary>
    public string? AppVersion { get; set; }

    /// <summary>Zeitpunkt der letzten Nachricht (Sortierung beider Listen)</summary>
    public DateTimeOffset LastMessageAt { get; set; }

    public bool HasUnreadForUser { get; set; }

    public bool HasUnreadForTeam { get; set; }

    public ICollection<FeedbackMessage> Messages { get; set; } = [];
}
