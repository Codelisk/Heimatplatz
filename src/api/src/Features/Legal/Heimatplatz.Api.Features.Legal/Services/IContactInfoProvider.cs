using Heimatplatz.Api.Features.Legal.Contracts.Models;

namespace Heimatplatz.Api.Features.Legal.Services;

/// <summary>
/// Laedt die zusammengefuehrten Kontaktdaten (Impressum + Contact-Zusatzfelder).
/// </summary>
public interface IContactInfoProvider
{
    /// <summary>
    /// Liefert null, solange kein aktives Impressum existiert - ohne Pflichtangaben gibt es
    /// keine sinnvollen Kontaktdaten.
    /// </summary>
    Task<LegalContactInfoDto?> GetAsync(CancellationToken cancellationToken = default);
}
