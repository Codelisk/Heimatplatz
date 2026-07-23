using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Kontakt in der Marketing-Kontaktdatenbank (potentielle Kunden: Makler,
/// Hausverwaltungen, Gemeinden, Partner, ...). Wird beim Versand einer Marketing-Mail
/// automatisch angelegt (Source "Versand"), sofern die Adresse noch unbekannt ist, oder
/// aus dem Firmenpool uebernommen (Source "Firmenbuch").
/// </summary>
public class MarketingContact : BaseEntity
{
    /// <summary>
    /// Normalisiert (lowercase, getrimmt) - eindeutig pro Kontakt, sofern gesetzt.
    /// Optional, weil Kontakte aus dem Firmenpool ohne Adresse entstehen: das Firmenbuch
    /// fuehrt keine Kontaktdaten, die Adresse kommt erst beim Telefonat dazu.
    /// </summary>
    public string? Email { get; set; }

    public string? Name { get; set; }

    public string? Company { get; set; }

    public string? Phone { get; set; }

    /// <summary>Sitz/Ort - aus dem Firmenbuch uebernommen, sonst manuell</summary>
    public string? City { get; set; }

    public MarketingContactType ContactType { get; set; } = MarketingContactType.Unknown;

    public MarketingContactStatus Status { get; set; } = MarketingContactStatus.Lead;

    /// <summary>Freitext-Notizen (Gespraechsverlauf, Vereinbarungen, ...)</summary>
    public string? Notes { get; set; }

    /// <summary>Woher der Kontakt stammt, z.B. "Manuell", "Versand", "Firmenbuch"</summary>
    public string? Source { get; set; }

    /// <summary>
    /// Firmenbuchnummer der Quell-Firma, wenn der Kontakt aus dem Firmenpool stammt.
    /// Eindeutig (sofern gesetzt) - macht die Uebernahme idempotent und blendet bereits
    /// uebernommene Firmen im Pool aus.
    /// </summary>
    public string? FirmenbuchFnr { get; set; }

    /// <summary>Vereinbarte Wiedervorlage - treibt die Faellig-Liste im Intern-Bereich</summary>
    public DateTimeOffset? NextFollowUpAt { get; set; }

    /// <summary>Letzter Marketing-Mail-Versand bzw. letztes Telefonat mit diesem Kontakt</summary>
    public DateTimeOffset? LastContactedAt { get; set; }

    /// <summary>Letzte eingegangene Antwort dieses Kontakts</summary>
    public DateTimeOffset? LastReplyAt { get; set; }

    public ICollection<MarketingEmail> Emails { get; set; } = [];

    public ICollection<MarketingInboundEmail> InboundEmails { get; set; } = [];

    public ICollection<MarketingActivity> Activities { get; set; } = [];
}
