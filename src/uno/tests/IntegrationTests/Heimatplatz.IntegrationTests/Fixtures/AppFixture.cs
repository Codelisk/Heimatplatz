using Heimatplatz.Core.Startup;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.IntegrationTests.Fixtures;

/// <summary>
/// Fixture fuer gemeinsame App-Instanz in Integration-Tests.
/// </summary>
public class AppFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;

    /// <summary>
    /// Der ServiceProvider fuer Tests.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Erstellt eine neue AppFixture mit konfigurierten Services.
    /// </summary>
    public AppFixture()
    {
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Konfiguriert die Services fuer die Fixture.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddAppServices();
    }

    /// <summary>
    /// Holt einen Service aus dem Container.
    /// </summary>
    public T GetService<T>() where T : notnull
    {
        return _serviceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Disposed die Fixture.
    /// </summary>
    public void Dispose()
    {
        _serviceProvider.Dispose();
        GC.SuppressFinalize(this);
    }
}
