using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Locations.Data.Entities;

/// <summary>
/// Gemeinde (z.B. Traun, Leonding, Ansfelden)
/// </summary>
public class Municipality : BaseEntity
{
    /// <summary>
    /// Offizieller Gemeindecode (z.B. "41001")
    /// </summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gemeindecode
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Name der Gemeinde (z.B. "Traun")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Postleitzahl
    /// </summary>
    public string PostalCode { get; set; } = string.Empty;

    /// <summary>
    /// Status (z.B. "Stadtgemeinde", "Marktgemeinde" oder leer)
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// FK zum Bezirk
    /// </summary>
    public Guid DistrictId { get; set; }

    /// <summary>
    /// Navigation Property zum Bezirk
    /// </summary>
    public District District { get; set; } = null!;
}
