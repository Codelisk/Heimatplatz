using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Immobilien.Data.Entities;

/// <summary>
/// Bild einer Immobilie
/// </summary>
public class ImmobilieBild : BaseEntity
{
    /// <summary>Fremdschluessel zur Immobilie</summary>
    public Guid ImmobilieId { get; set; }

    /// <summary>URL zum Bild (CDN/Storage)</summary>
    public required string Url { get; set; }

    /// <summary>Alternativer Text fuer Barrierefreiheit</summary>
    public string? AltText { get; set; }

    /// <summary>Sortierreihenfolge (niedrig = zuerst)</summary>
    public int Reihenfolge { get; set; }

    /// <summary>Kennzeichnet das Hauptbild fuer Cards</summary>
    public bool IstHauptbild { get; set; }

    /// <summary>Navigation zur Immobilie</summary>
    public Immobilie Immobilie { get; set; } = null!;
}
