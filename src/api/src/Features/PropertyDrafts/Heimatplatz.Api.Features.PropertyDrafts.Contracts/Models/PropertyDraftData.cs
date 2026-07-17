using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;

/// <summary>
/// Vollstaendiger Wizard-Zustand eines Inserat-Entwurfs. Wird serverseitig als
/// JSON-Blob gespeichert, damit neue Felder KEINE Migration brauchen.
/// Alle Felder sind optional - ein Entwurf darf beliebig unvollstaendig sein;
/// validiert wird erst beim Veroeffentlichen (PublishPropertyDraft -> CreateProperty).
/// Note: Class mit Default-Properties (kein record) fuer den Shiny Mediator OpenAPI-Generator.
/// </summary>
public class PropertyDraftData
{
    /// <summary>Version des Payload-Schemas (fuer tolerante Deserialisierung aelterer Entwuerfe)</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Zuletzt aktiver Wizard-Schritt (0-basiert)</summary>
    public int StepIndex { get; set; }

    // Schritt 1: Medien (bereits hochgeladen, nur URLs)
    public List<string>? ImageUrls { get; set; }
    public List<string>? VideoUrls { get; set; }

    // Schritt 2: Beschreibung/Diktat + KI-Zustand
    public string? DictatedText { get; set; }
    public bool AiSkipped { get; set; }
    public Guid? AnalysisId { get; set; }

    /// <summary>KI-Ergebnis wurde bereits in die Felder uebernommen (Resume darf nicht erneut ueberschreiben)</summary>
    public bool AnalysisApplied { get; set; }

    // Schritt 3: Lage & Preis
    public string? Address { get; set; }
    public Guid? MunicipalityId { get; set; }

    /// <summary>Anzeige-Text der gewaehlten Gemeinde, z.B. "Linz (4020)" - Restore ohne Lookup</summary>
    public string? MunicipalityDisplay { get; set; }
    public decimal? Price { get; set; }

    // Schritt 4: Eckdaten
    public PropertyType? Type { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public int? Rooms { get; set; }
    public int? LivingAreaSquareMeters { get; set; }
    public int? PlotAreaSquareMeters { get; set; }
    public int? YearBuilt { get; set; }
    public List<string>? Features { get; set; }

    /// <summary>Zusammenfassung aus der KI-Analyse (nur Anzeige)</summary>
    public string? AiSummary { get; set; }
}
