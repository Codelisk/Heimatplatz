using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Locations.Data.Entities;

/// <summary>
/// Politischer Bezirk (z.B. Linz-Land, Wels-Land)
/// </summary>
public class District : BaseEntity
{
    /// <summary>
    /// Offizieller Schluessel (z.B. "410" fuer Linz-Land)
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Bezirkscode
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Name des Bezirks (z.B. "Linz-Land")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// FK zum Bundesland
    /// </summary>
    public Guid FederalProvinceId { get; set; }

    /// <summary>
    /// Navigation Property zum Bundesland
    /// </summary>
    public FederalProvince FederalProvince { get; set; } = null!;

    /// <summary>
    /// Zugehoerige Gemeinden
    /// </summary>
    public List<Municipality> Municipalities { get; set; } = [];
}
