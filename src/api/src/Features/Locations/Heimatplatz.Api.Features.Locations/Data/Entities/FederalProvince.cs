using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Locations.Data.Entities;

/// <summary>
/// Bundesland (z.B. Oberoesterreich, Wien, Steiermark)
/// </summary>
public class FederalProvince : BaseEntity
{
    /// <summary>
    /// Offizieller Schluessel von Statistik Austria (1-9)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Name des Bundeslandes (z.B. "Oberoesterreich")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Zugehoerige Bezirke
    /// </summary>
    public List<District> Districts { get; set; } = [];
}
