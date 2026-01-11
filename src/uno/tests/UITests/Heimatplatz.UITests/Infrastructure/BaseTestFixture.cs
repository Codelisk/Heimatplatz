using Heimatplatz.UITests.Configuration;
using Heimatplatz.UITests.Helpers;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using Uno.UITest;

namespace Heimatplatz.UITests.Infrastructure;

/// <summary>
/// Basis-Testklasse fuer alle UI-Tests.
/// Stellt App-Initialisierung, Screenshot-Capture und gemeinsame Hilfsmethoden bereit.
/// </summary>
[TestFixture]
public abstract class BaseTestFixture
{
    /// <summary>
    /// Die aktuelle App-Instanz.
    /// </summary>
    protected IApp App { get; private set; } = null!;

    /// <summary>
    /// Screenshot-Helper fuer Test-Dokumentation.
    /// </summary>
    protected ScreenshotHelper Screenshots { get; private set; } = null!;

    /// <summary>
    /// Wait-Helper fuer Element-Waits.
    /// </summary>
    protected WaitHelper Wait { get; private set; } = null!;

    /// <summary>
    /// Die aktuelle Test-Plattform.
    /// </summary>
    protected Platform CurrentPlatform => Constants.CurrentPlatform;

    /// <summary>
    /// Wird vor jedem Test aufgerufen. Startet die App.
    /// </summary>
    [SetUp]
    public virtual void SetUp()
    {
        App = AppInitializer.StartApp();
        Screenshots = new ScreenshotHelper(App);
        Wait = new WaitHelper(App);

        OnSetUp();
    }

    /// <summary>
    /// Wird nach jedem Test aufgerufen. Erstellt Screenshot bei Fehler.
    /// </summary>
    [TearDown]
    public virtual void TearDown()
    {
        try
        {
            // Screenshot bei fehlgeschlagenem Test
            if (TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                var testName = TestContext.CurrentContext.Test.Name;
                Screenshots.TakeScreenshot($"FAILED_{testName}");
            }

            OnTearDown();
        }
        finally
        {
            AppInitializer.StopApp();
        }
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
    /// Wartet auf ein Element mit dem angegebenen AutomationId.
    /// </summary>
    protected void WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        Wait.ForElement(automationId, timeout);
    }

    /// <summary>
    /// Wartet bis ein Element nicht mehr sichtbar ist.
    /// </summary>
    protected void WaitForNoElement(string automationId, TimeSpan? timeout = null)
    {
        Wait.ForNoElement(automationId, timeout);
    }

    /// <summary>
    /// Prueft ob ein Element existiert.
    /// </summary>
    protected bool ElementExists(string automationId)
    {
        return App.Query(automationId).Any();
    }

    /// <summary>
    /// Tippt auf ein Element.
    /// </summary>
    protected void Tap(string automationId)
    {
        WaitForElement(automationId);
        App.Tap(automationId);
    }

    /// <summary>
    /// Gibt Text in ein Element ein.
    /// </summary>
    protected void EnterText(string automationId, string text)
    {
        WaitForElement(automationId);
        App.ClearText(automationId);
        App.EnterText(automationId, text);
    }

    /// <summary>
    /// Liest den Text eines Elements.
    /// </summary>
    protected string GetText(string automationId)
    {
        WaitForElement(automationId);
        return App.Query(automationId).FirstOrDefault()?.Text ?? string.Empty;
    }
}
