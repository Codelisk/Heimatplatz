using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Infrastructure;

/// <summary>
/// Basis-Testklasse fuer alle API Unit-Tests.
/// </summary>
[TestFixture]
public abstract class BaseApiUnitTest
{
    /// <summary>
    /// Wird vor jedem Test aufgerufen.
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        OnSetUp();
    }

    /// <summary>
    /// Wird nach jedem Test aufgerufen.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        OnTearDown();
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
