using Heimatplatz.Core.Startup;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.IntegrationTests.Fixtures;

/// <summary>
/// Fixture fuer gemeinsame App-Instanz in Integration-Tests.
/// </summary>
public class AppFixture : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Der ServiceProvider fuer Tests.
    /// </summary>
    public IServiceProvider ServiceProvider => _serviceProvider;

    /// <summary>
    /// Die Konfiguration fuer Tests.
    /// </summary>
    public IConfiguration Configuration => _configuration;

    /// <summary>
    /// Erstellt eine neue AppFixture mit konfigurierten Services.
    /// </summary>
    public AppFixture()
    {
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mediator:Http:Heimatplatz.Core.ApiClient.Generated.*"] = "http://localhost:5292"
            })
            .Build();

        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    /// <summary>
    /// Konfiguriert die Services fuer die Fixture.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        services.AddAppServices(_configuration);
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
