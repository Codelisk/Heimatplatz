using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.Api;

/// <summary>
/// API-spezifische DI-Konstanten.
/// </summary>
public static class ApiService
{
    /// <summary>
    /// Default: Scoped für API (pro Request).
    /// </summary>
    public const ServiceLifetime Lifetime = ServiceLifetime.Scoped;

    /// <summary>
    /// Immer TryAdd verwenden.
    /// </summary>
    public const bool TryAdd = true;
}
