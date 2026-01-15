using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.UnitTests.Mocks;

/// <summary>
/// Helper zum Erstellen eines Mock-ServiceProviders fuer Unit-Tests.
/// </summary>
public static class MockServiceProvider
{
    /// <summary>
    /// Erstellt einen ServiceProvider mit den angegebenen Services.
    /// </summary>
    /// <param name="configure">Konfiguration der Services.</param>
    /// <returns>Ein konfigurierter ServiceProvider.</returns>
    public static IServiceProvider Create(Action<IServiceCollection>? configure = null)
    {
        var services = new ServiceCollection();
        configure?.Invoke(services);
        return services.BuildServiceProvider();
    }

    /// <summary>
    /// Erstellt eine ServiceCollection fuer weitere Konfiguration.
    /// </summary>
    /// <returns>Eine neue ServiceCollection.</returns>
    public static IServiceCollection CreateServices()
    {
        return new ServiceCollection();
    }
}
