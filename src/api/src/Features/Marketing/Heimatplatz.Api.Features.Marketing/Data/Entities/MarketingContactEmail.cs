using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Zusaetzliche E-Mail-Adresse eines Kontakts neben <see cref="MarketingContact.Email"/>
/// (der Versand-Adresse). Firmen melden sich oft von persoenlichen Adressen der
/// Ansprechpartner zurueck (office@ angeschrieben, antwort von vorname.nachname@) - der
/// Posteingang-Sync ordnet ueber ALLE Adressen zu und lernt neue Absender-Adressen einer
/// bekannten Firmen-Domain automatisch dazu (Source "Posteingang").
/// </summary>
public class MarketingContactEmail : BaseEntity
{
    public Guid ContactId { get; set; }
    public MarketingContact Contact { get; set; } = null!;

    /// <summary>Normalisiert (lowercase, getrimmt) - eindeutig ueber alle Zusatzadressen</summary>
    public required string Email { get; set; }

    /// <summary>Woher die Adresse stammt: "Manuell" oder "Posteingang" (automatisch gelernt)</summary>
    public string? Source { get; set; }
}
