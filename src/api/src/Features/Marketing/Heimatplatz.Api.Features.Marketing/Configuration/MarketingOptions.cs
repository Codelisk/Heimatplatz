namespace Heimatplatz.Api.Features.Marketing.Configuration;

/// <summary>
/// Konfiguration des Marketing-Features (Section "Marketing").
/// Aktuell: KI-gestuetzte E-Mail-Erstellung fuer den Intern-Bereich.
/// </summary>
public class MarketingOptions
{
    public const string SectionName = "Marketing";

    /// <summary>
    /// Welcher KI-Provider die E-Mail-Texte erstellt:
    /// "Mock" (Dev, Platzhalter-Text ohne KI) oder
    /// "AiConnector" (externer AiConnector-Backend-Service, Workspace-basiert).
    /// </summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Einstellungen fuer den "AiConnector"-Provider</summary>
    public MarketingAiConnectorOptions AiConnector { get; set; } = new();

    /// <summary>Einstellungen des Firmenpools (Lead-Quelle Firmenbuch)</summary>
    public MarketingLeadPoolOptions LeadPool { get; set; } = new();
}

/// <summary>
/// Firmenpool: welche Firmenbuch-Eintraege als moegliche Immobilien-Kontakte gelten.
/// Der Firmenbuch-Katalog fuehrt weder Branche noch Kontaktdaten - der Firmenname ist
/// die einzige verfuegbare Eingrenzung.
/// </summary>
public class MarketingLeadPoolOptions
{
    /// <summary>
    /// Schlagworte, die im Firmennamen auf die Immobilienbranche hindeuten. Vergleich
    /// case-insensitiv als Teilstring; ein Treffer genuegt. Konfigurierbar, damit die
    /// Trefferliste ohne Deploy nachgeschaerft werden kann.
    /// "immo" deckt Immobilien/Immobilien-Marken mit ab, ist aber bewusst breit -
    /// nicht passende Firmen werden im Pool einfach nicht ausgewaehlt.
    /// </summary>
    public List<string> NameKeywords { get; set; } =
    [
        "immo",
        "liegenschaft",
        "hausverwalt",
        "bauträger",
        "bautraeger",
        "realität",
        "realitaet",
        "wohnbau",
        "makler",
        "wohnungseigentum"
    ];

    /// <summary>
    /// Schlagworte gegen den amtlichen Wortlaut der aktiven GISA-Gewerbeberechtigungen -
    /// findet Immobilienfirmen, deren Name die Branche nicht verraet (z.B. "Konzeptmühle
    /// GmbH" mit Immobilientreuhänder-Berechtigung). ODER-verknuepft mit den NameKeywords.
    /// "Immobilientreuhänder" deckt laut GISA-Bestand alle drei Teilgewerbe ab
    /// (Immobilienmakler, Immobilienverwalter, Bauträger) - Vorsicht bei Erweiterungen:
    /// "makler" allein traefe auch Versicherungsmakler.
    /// </summary>
    public List<string> GewerbeKeywords { get; set; } =
    [
        "Immobilientreuhänder"
    ];
}

/// <summary>
/// AiConnector-Einstellungen des Marketing-Features. Basis-URL und API-Key des
/// AiConnector-Backends werden zentral im Heimatplatz.Api.Core.AiConnectorClient
/// konfiguriert (Mediator:Http:... bzw. AiConnector:ApiKey).
/// </summary>
public class MarketingAiConnectorOptions
{
    /// <summary>Workspace, in dem der Prompt ausgefuehrt wird</summary>
    public string WorkspaceId { get; set; } = "projects/heimatplatz";

    /// <summary>
    /// Section-Verzeichnis innerhalb des Workspaces mit den Regeln fuer
    /// Marketing-E-Mails (AGENTS.md mit Rolle + Ausgabeformat). Der Prompt
    /// referenziert diese Datei explizit - so skalieren weitere Marketing-Aufgaben
    /// spaeter ueber eigene Section-Verzeichnisse (z.B. sections/marketing/social).
    /// </summary>
    public string SectionPath { get; set; } = "sections/marketing/email";

    /// <summary>Optionales Claude-Modell (leer = Default des AiConnectors)</summary>
    public string? Model { get; set; }
}
