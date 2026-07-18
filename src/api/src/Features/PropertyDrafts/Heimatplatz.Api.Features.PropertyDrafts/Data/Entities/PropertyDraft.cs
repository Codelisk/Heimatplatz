using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.PropertyDrafts.Contracts.Models;

namespace Heimatplatz.Api.Features.PropertyDrafts.Data.Entities;

/// <summary>
/// Server-seitiger Entwurf einer angefangenen Immobilie aus dem Erstellungs-Wizard.
/// Bewusst KEINE Property-Zeile: das PropertyChange-Journal, der Delta-Sync und die
/// oeffentlichen Abfragen sehen Entwuerfe dadurch nie.
/// Typisierte Summary-Spalten dienen nur der Listen-Anzeige; der vollstaendige
/// Wizard-Zustand liegt als JSON in <see cref="PayloadJson"/> (Feldaenderungen am
/// Payload brauchen daher keine Migration).
/// Die Description*-Spalten gehoeren dem Beschreibungs-Hintergrund-Job und liegen
/// bewusst NICHT im Payload: Client-Auto-Saves (kompletter Payload-Upsert) koennen
/// den Job-Fortschritt so nie ueberschreiben.
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

    /// <summary>Serialisiertes PropertyDraftData (kompletter Wizard-Zustand)</summary>
    public string PayloadJson { get; set; } = "{}";

    /// <summary>Zustand der asynchronen KI-Beschreibungs-Generierung</summary>
    public DraftDescriptionStatus DescriptionStatus { get; set; } = DraftDescriptionStatus.None;

    /// <summary>Die fuer den laufenden/letzten Job eingereichten Stichwoerter</summary>
    public string? DescriptionKeywords { get; set; }

    /// <summary>Vom Job erstellte Beschreibung (nur bei Status Finished)</summary>
    public string? GeneratedDescription { get; set; }

    /// <summary>Fehlermeldung des Jobs (nur bei Status Failed)</summary>
    public string? DescriptionError { get; set; }

    public DateTimeOffset? DescriptionRequestedAt { get; set; }

    public DateTimeOffset? DescriptionCompletedAt { get; set; }

    /// <summary>
    /// Gesetzt, wenn der Entwurf veroeffentlicht wurde, waehrend der Beschreibungs-Job
    /// noch lief: Die Immobilie ist mit Platzhalter-Beschreibung live, der fertige Text
    /// wird vom Job nachgeliefert. Solche Entwuerfe erscheinen nicht mehr in der Liste
    /// und werden nach der Nachlieferung geloescht.
    /// </summary>
    public Guid? PublishedPropertyId { get; set; }
}
