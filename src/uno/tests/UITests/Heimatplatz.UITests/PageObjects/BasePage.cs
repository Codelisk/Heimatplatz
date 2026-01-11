using Heimatplatz.UITests.Configuration;
using Uno.UITest;

namespace Heimatplatz.UITests.PageObjects;

/// <summary>
/// Abstrakte Basisklasse fuer alle Page Objects.
/// Stellt gemeinsame Funktionalitaet fuer Element-Interaktion bereit.
/// </summary>
public abstract class BasePage
{
    /// <summary>
    /// Die App-Instanz fuer UI-Interaktionen.
    /// </summary>
    protected readonly IApp App;

    /// <summary>
    /// Erstellt eine neue Page-Instanz.
    /// </summary>
    protected BasePage(IApp app)
    {
        App = app ?? throw new ArgumentNullException(nameof(app));
    }

    /// <summary>
    /// Wartet auf ein Element und gibt es zurueck.
    /// </summary>
    protected void WaitForElement(string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        App.WaitForElement(automationId, timeout: wait);
    }

    /// <summary>
    /// Wartet bis ein Element nicht mehr sichtbar ist.
    /// </summary>
    protected void WaitForNoElement(string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        App.WaitForNoElement(automationId, timeout: wait);
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
    /// Gibt Text in ein Eingabefeld ein.
    /// </summary>
    protected void EnterText(string automationId, string text)
    {
        WaitForElement(automationId);
        App.ClearText(automationId);
        App.EnterText(automationId, text);
    }

    /// <summary>
    /// Loescht den Text eines Eingabefeldes.
    /// </summary>
    protected void ClearText(string automationId)
    {
        WaitForElement(automationId);
        App.ClearText(automationId);
    }

    /// <summary>
    /// Liest den Text eines Elements.
    /// </summary>
    protected string GetText(string automationId)
    {
        WaitForElement(automationId);
        var element = App.Query(automationId).FirstOrDefault();
        return element?.Text ?? string.Empty;
    }

    /// <summary>
    /// Prueft ob ein Element aktiviert ist.
    /// </summary>
    protected bool IsEnabled(string automationId)
    {
        WaitForElement(automationId);
        var element = App.Query(automationId).FirstOrDefault();
        return element?.Enabled ?? false;
    }

    /// <summary>
    /// Erstellt einen Screenshot mit dem angegebenen Namen.
    /// </summary>
    protected void TakeScreenshot(string name)
    {
        App.Screenshot(name);
    }

    /// <summary>
    /// Scrollt zu einem Element.
    /// </summary>
    protected void ScrollTo(string automationId)
    {
        App.ScrollDownTo(automationId);
    }

    /// <summary>
    /// Wartet auf die Seite. Kann in abgeleiteten Klassen ueberschrieben werden.
    /// </summary>
    public virtual void WaitForPage()
    {
        // Standardimplementierung: nichts tun
        // Abgeleitete Klassen sollten auf spezifische Elemente warten
    }
}
