using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz;

/// <summary>
/// Uno-spezifische DI-Konstanten.
/// </summary>
public static class UnoService
{
    /// <summary>
    /// Default: Singleton für Uno/Client-Apps.
    /// </summary>
    public const ServiceLifetime Lifetime = ServiceLifetime.Singleton;

    /// <summary>
    /// Immer TryAdd verwenden.
    /// </summary>
    public const bool TryAdd = true;
}
