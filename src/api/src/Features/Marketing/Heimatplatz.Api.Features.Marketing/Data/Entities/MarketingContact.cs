using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Kontakt in der Marketing-Kontaktdatenbank (potentielle Kunden: Makler,
/// Hausverwaltungen, Gemeinden, Partner, ...). Wird beim Versand einer Marketing-Mail
/// automatisch angelegt (Source "Versand"), sofern die Adresse noch unbekannt ist.
/// </summary>
public class MarketingContact : BaseEntity
{
    /// <summary>Normalisiert (lowercase, getrimmt) - eindeutig pro Kontakt</summary>
    public required string Email { get; set; }

    public string? Name { get; set; }

    public string? Company { get; set; }

    public string? Phone { get; set; }

    public MarketingContactType ContactType { get; set; } = MarketingContactType.Unknown;

    public MarketingContactStatus Status { get; set; } = MarketingContactStatus.Lead;

    /// <summary>Freitext-Notizen (Gespraechsverlauf, Vereinbarungen, ...)</summary>
    public string? Notes { get; set; }

    /// <summary>Woher der Kontakt stammt, z.B. "Manuell", "Versand", "Import"</summary>
    public string? Source { get; set; }

    /// <summary>Letzter Marketing-Mail-Versand an diesen Kontakt</summary>
    public DateTimeOffset? LastContactedAt { get; set; }

    /// <summary>Letzte eingegangene Antwort dieses Kontakts</summary>
    public DateTimeOffset? LastReplyAt { get; set; }

    public ICollection<MarketingEmail> Emails { get; set; } = [];

    public ICollection<MarketingInboundEmail> InboundEmails { get; set; } = [];
}
