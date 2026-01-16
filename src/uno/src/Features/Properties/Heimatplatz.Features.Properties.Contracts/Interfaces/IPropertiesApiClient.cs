using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Contracts.Interfaces;

/// <summary>
/// API-Client fuer Immobilien-Operationen
/// </summary>
public interface IPropertiesApiClient
{
    /// <summary>
    /// Erstellt eine neue Immobilie
    /// </summary>
    Task<CreatePropertyResponseDto> CreatePropertyAsync(CreatePropertyDto property);

    /// <summary>
    /// Ruft alle Immobilien ab
    /// </summary>
    Task<List<PropertyListItemDto>> GetPropertiesAsync();

    /// <summary>
    /// Ruft eine einzelne Immobilie ab
    /// </summary>
    Task<PropertyDetailDto> GetPropertyByIdAsync(Guid id);
}
