using Microsoft.AspNetCore.Mvc.Testing;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Basis-Testklasse fuer alle API Integration-Tests.
/// </summary>
[TestFixture]
public abstract class BaseApiIntegrationTest
{
    /// <summary>
    /// Die WebApplicationFactory fuer den Test.
    /// </summary>
    protected WebApplicationFactory<Program> Factory { get; private set; } = null!;

    /// <summary>
    /// Der HttpClient fuer API-Aufrufe.
    /// </summary>
    protected HttpClient Client { get; private set; } = null!;

    /// <summary>
    /// Wird vor jedem Test aufgerufen.
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        Factory = CreateFactory();
        Client = Factory.CreateClient();
        OnSetUp();
    }

    /// <summary>
    /// Wird nach jedem Test aufgerufen.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        OnTearDown();
        Client?.Dispose();
        Factory?.Dispose();
    }

    /// <summary>
    /// Erstellt die WebApplicationFactory.
    /// Kann ueberschrieben werden fuer spezifische Konfiguration.
    /// </summary>
    protected virtual WebApplicationFactory<Program> CreateFactory()
    {
        return new CustomWebApplicationFactory<Program>();
    }

    /// <summary>
    /// Kann in abgeleiteten Klassen ueberschrieben werden fuer zusaetzliches Setup.
    /// </summary>
    protected virtual void OnSetUp() { }

    /// <summary>
    /// Kann in abgeleiteten Klassen ueberschrieben werden fuer zusaetzliches TearDown.
    /// </summary>
    protected virtual void OnTearDown() { }
}
