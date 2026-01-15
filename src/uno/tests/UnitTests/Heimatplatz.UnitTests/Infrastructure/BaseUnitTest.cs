using NUnit.Framework;

namespace Heimatplatz.UnitTests.Infrastructure;

/// <summary>
/// Basis-Testklasse fuer alle Unit-Tests.
/// Stellt gemeinsame Setup- und TearDown-Logik bereit.
/// </summary>
[TestFixture]
public abstract class BaseUnitTest
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
