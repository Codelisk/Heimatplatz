using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Ein Schritt in der Bearbeitung eines Kontakts: Anruf, Notiz, Statuswechsel,
/// Wiedervorlage oder Termin. Zusammen mit <see cref="MarketingEmail"/> und
/// <see cref="MarketingInboundEmail"/> ergibt das die lueckenlose Kontakt-Timeline.
/// Gesendete Mails erzeugen bewusst KEINE Aktivitaet - sie sind bereits eine eigene
/// Entitaet und wuerden sonst doppelt in der Timeline stehen.
/// </summary>
public class MarketingActivity : BaseEntity
{
    public Guid ContactId { get; set; }

    public MarketingContact Contact { get; set; } = null!;

    public MarketingActivityType Type { get; set; }

    /// <summary>Gespraechsnotiz bzw. Freitext</summary>
    public string? Notes { get; set; }

    /// <summary>Nur bei <see cref="MarketingActivityType.StatusChange"/> gesetzt</summary>
    public MarketingContactStatus? StatusFrom { get; set; }

    /// <summary>Nur bei <see cref="MarketingActivityType.StatusChange"/> gesetzt</summary>
    public MarketingContactStatus? StatusTo { get; set; }

    /// <summary>Der bei diesem Schritt vereinbarte Wiedervorlage-Termin</summary>
    public DateTimeOffset? FollowUpAt { get; set; }

    /// <summary>
    /// Zeitpunkt des Ereignisses - nicht zwingend die Anlagezeit, damit zurueckliegende
    /// Telefonate nachgetragen werden koennen.
    /// </summary>
    public DateTimeOffset OccurredAt { get; set; }
}
