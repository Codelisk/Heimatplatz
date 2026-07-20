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
    /// <summary>
    /// Version des Payload-Schemas (fuer tolerante Deserialisierung aelterer Entwuerfe).
    /// Version 2: manueller Wizard-Fluss (Fotos -> Eckdaten -> Lage/Preis -> Beschreibung),
    /// KI-Analyse-Felder (DictatedText/AnalysisId/...) entfernt, Beschreibungs-Generierung
    /// laeuft ueber eigene Entwurfs-Spalten + Hintergrund-Job.
    /// Version 3: optionaler Ansprechpartner (ContactName/ContactEmail/ContactPhone).
    /// </summary>
    public int SchemaVersion { get; set; } = 3;

    /// <summary>Zuletzt aktiver Wizard-Schritt (0-basiert)</summary>
    public int StepIndex { get; set; }

    // Schritt 1: Fotos (bereits hochgeladen, nur URLs)
    public List<string>? ImageUrls { get; set; }

    /// <summary>
    /// Nur noch Altbestand (Schema 1 nutzte Videos fuer die KI-Analyse). Der Wizard bietet
    /// keinen Video-Upload mehr an; das Loeschen eines Entwurfs raeumt die Dateien weiter ab.
    /// </summary>
    public List<string>? VideoUrls { get; set; }

    // Schritt 2: Eckdaten
    public PropertyType? Type { get; set; }
    public string? Title { get; set; }
    public int? Rooms { get; set; }
    public int? LivingAreaSquareMeters { get; set; }
    public int? PlotAreaSquareMeters { get; set; }
    public int? YearBuilt { get; set; }
    public List<string>? Features { get; set; }

    // Schritt 3: Lage & Preis
    public string? Address { get; set; }
    public Guid? MunicipalityId { get; set; }

    /// <summary>Anzeige-Text der gewaehlten Gemeinde, z.B. "Linz (4020)" - Restore ohne Lookup</summary>
    public string? MunicipalityDisplay { get; set; }
    public decimal? Price { get; set; }

    /// <summary>Link zum Originalinserat (landet beim Veroeffentlichen am Erst-Kontakt)</summary>
    public string? OriginalListingUrl { get; set; }

    // Optionaler Ansprechpartner (landet beim Veroeffentlichen als Kontakt mit DisplayOrder 1)
    public string? ContactName { get; set; }
    public string? ContactEmail { get; set; }
    public string? ContactPhone { get; set; }

    // Schritt 4: Beschreibung
    /// <summary>Vom Nutzer gewaehlter Beschreibungs-Modus (Wiederherstellen der Auswahl)</summary>
    public DraftDescriptionMode DescriptionMode { get; set; }

    /// <summary>Finaler Beschreibungstext (manuell geschrieben oder generierter Text nach Uebernahme/Bearbeitung)</summary>
    public string? Description { get; set; }

    /// <summary>
    /// Stichwoerter/Diktat fuer die Generierung, solange noch kein Job angefordert wurde.
    /// Nach dem Anfordern haelt der Server die eingereichten Stichwoerter in einer eigenen Spalte.
    /// </summary>
    public string? DescriptionKeywords { get; set; }
}

/// <summary>
/// Wie der Nutzer die Beschreibung im Wizard erstellt.
/// </summary>
public enum DraftDescriptionMode
{
    /// <summary>Noch keine Auswahl getroffen</summary>
    None = 0,

    /// <summary>Beschreibung wird manuell geschrieben</summary>
    Manual = 1,

    /// <summary>Beschreibung wird aus Stichwoertern/Diktat generiert (Hintergrund-Job)</summary>
    Generate = 2
}
