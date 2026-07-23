using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Wiederverwendbare E-Mail-Vorlage fuer den Erstkontakt und Follow-ups. Betreff und Text
/// enthalten Platzhalter (MarketingTemplatePlaceholders), die beim Auswaehlen aus dem
/// Kontakt befuellt werden. Ueber den Intern-Bereich pflegbar - Textaenderungen brauchen
/// bewusst keinen Deploy.
/// Die Signatur ist NICHT Teil der Vorlage; sie kommt wie beim freien Versand aus den
/// Kontakt-Stammdaten (MarketingEmailComposer).
/// </summary>
public class MarketingEmailTemplate : BaseEntity
{
    /// <summary>Anzeigename in der Auswahl, eindeutig</summary>
    public required string Name { get; set; }

    /// <summary>Kurze Erklaerung, wofuer die Vorlage gedacht ist</summary>
    public string? Description { get; set; }

    public required string Subject { get; set; }

    /// <summary>Fliesstext ohne Signatur</summary>
    public required string Body { get; set; }

    /// <summary>Inaktive Vorlagen bleiben erhalten, erscheinen aber nicht in der Auswahl</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Sortierung in der Auswahl (kleiner zuerst), bei Gleichstand nach Name</summary>
    public int DisplayOrder { get; set; }
}
