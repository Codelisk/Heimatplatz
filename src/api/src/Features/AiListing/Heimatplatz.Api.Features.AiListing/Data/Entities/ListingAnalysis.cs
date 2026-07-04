using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.AiListing.Contracts.Enums;

namespace Heimatplatz.Api.Features.AiListing.Data.Entities;

/// <summary>
/// Ein asynchroner KI-Analyse-Job fuer die Inseratserstellung.
/// Haelt Eingaben (Medien-URLs, Diktat) und das Ergebnis als JSON.
/// </summary>
public class ListingAnalysis : BaseEntity
{
    public Guid UserId { get; set; }

    public ListingAnalysisStatus Status { get; set; } = ListingAnalysisStatus.Queued;

    /// <summary>URLs der hochgeladenen Fotos (JSON-Column)</summary>
    public List<string> ImageUrls { get; set; } = [];

    /// <summary>URLs der hochgeladenen Videos (JSON-Column)</summary>
    public List<string> VideoUrls { get; set; } = [];

    /// <summary>Per Sprache diktierte Beschreibung des Nutzers</summary>
    public string? DictatedText { get; set; }

    /// <summary>Zusaetzliche manuelle Notizen des Nutzers</summary>
    public string? UserNotes { get; set; }

    /// <summary>Serialisiertes ExtractedListingData (nur bei Status Finished)</summary>
    public string? ResultJson { get; set; }

    /// <summary>Fehlerbeschreibung (nur bei Status Failed)</summary>
    public string? ErrorMessage { get; set; }

    public DateTimeOffset? StartedAt { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }
}
