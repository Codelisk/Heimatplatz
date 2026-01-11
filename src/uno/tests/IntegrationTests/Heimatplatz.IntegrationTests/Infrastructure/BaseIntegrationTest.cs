using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Heimatplatz.IntegrationTests.Infrastructure;

/// <summary>
/// Basis-Testklasse fuer alle Integration-Tests.
/// Stellt einen echten DI-Container bereit.
/// </summary>
[TestFixture]
public abstract class BaseIntegrationTest
{
    /// <summary>
    /// Der ServiceProvider fuer den Test.
    /// </summary>
    protected IServiceProvider ServiceProvider { get; private set; } = null!;

    /// <summary>
    /// Die ServiceCollection fuer Konfiguration.
    /// </summary>
    protected IServiceCollection Services { get; private set; } = null!;

    /// <summary>
    /// Wird vor jedem Test aufgerufen.
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        Services = new ServiceCollection();
        ConfigureServices(Services);
        ServiceProvider = Services.BuildServiceProvider();
        OnSetUp();
    }

    /// <summary>
    /// Wird nach jedem Test aufgerufen.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        OnTearDown();

        if (ServiceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    /// <summary>
    /// Konfiguriert die Services fuer den Test.
    /// </summary>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
        // Basis-Konfiguration - kann in abgeleiteten Klassen erweitert werden
    }

    /// <summary>
    /// Kann in abgeleiteten Klassen ueberschrieben werden fuer zusaetzliches Setup.
    /// </summary>
    protected virtual void OnSetUp() { }

    /// <summary>
    /// Kann in abgeleiteten Klassen ueberschrieben werden fuer zusaetzliches TearDown.
    /// </summary>
    protected virtual void OnTearDown() { }

    /// <summary>
    /// Holt einen Service aus dem DI-Container.
    /// </summary>
    protected T GetService<T>() where T : notnull
    {
        return ServiceProvider.GetRequiredService<T>();
    }

    /// <summary>
    /// Versucht einen Service aus dem DI-Container zu holen.
    /// </summary>
    protected T? GetOptionalService<T>() where T : class
    {
        return ServiceProvider.GetService<T>();
    }
}
