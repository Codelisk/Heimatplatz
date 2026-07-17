using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;

namespace Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;

/// <summary>
/// Server-seitiger Entwurf einer angefangenen Immobilie aus dem Erstellungs-Wizard.
/// Bewusst KEINE Property-Zeile: das PropertyChange-Journal, der Delta-Sync und die
/// oeffentlichen Abfragen sehen Entwuerfe dadurch nie.
/// Typisierte Summary-Spalten dienen nur der Listen-Anzeige; der vollstaendige
/// Wizard-Zustand liegt als JSON in <see cref="PayloadJson"/> (Feldaenderungen am
/// Payload brauchen daher keine Migration).
/// </summary>
public class PropertyDraft : BaseEntity
{
    public Guid UserId { get; set; }

    /// <summary>Version des Payload-Schemas (tolerante Deserialisierung aelterer Entwuerfe)</summary>
    public int SchemaVersion { get; set; } = 1;

    /// <summary>Zuletzt aktiver Wizard-Schritt (0-basiert)</summary>
    public int StepIndex { get; set; }

    /// <summary>Titel fuer die Listen-Anzeige (aus dem Payload abgeleitet)</summary>
    public string? Title { get; set; }

    public PropertyType? Type { get; set; }

    /// <summary>Thumbnail fuer die Entwurfs-Karte (erstes hochgeladenes Foto)</summary>
    public string? FirstImageUrl { get; set; }

    /// <summary>Laufende/abgeschlossene KI-Analyse (fuer Re-Polling beim Fortsetzen)</summary>
    public Guid? AnalysisId { get; set; }

    /// <summary>Serialisiertes PropertyDraftData (kompletter Wizard-Zustand)</summary>
    public string PayloadJson { get; set; } = "{}";
}
