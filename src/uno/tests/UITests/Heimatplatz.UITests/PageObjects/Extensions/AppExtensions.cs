using Heimatplatz.UITests.Configuration;
using Uno.UITest;

namespace Heimatplatz.UITests.PageObjects.Extensions;

/// <summary>
/// Erweiterungsmethoden fuer IApp.
/// </summary>
public static class AppExtensions
{
    /// <summary>
    /// Wartet auf ein Element mit dem angegebenen AutomationId.
    /// </summary>
    public static void WaitForAutomationId(this IApp app, string automationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.DefaultTimeout;
        app.WaitForElement(automationId, timeout: wait);
    }

    /// <summary>
    /// Tippt auf ein Element und wartet vorher darauf.
    /// </summary>
    public static void TapAndWait(this IApp app, string automationId, TimeSpan? timeout = null)
    {
        app.WaitForAutomationId(automationId, timeout);
        app.Tap(automationId);
    }

    /// <summary>
    /// Gibt Text ein und wartet vorher auf das Element.
    /// </summary>
    public static void EnterTextAndWait(this IApp app, string automationId, string text, TimeSpan? timeout = null)
    {
        app.WaitForAutomationId(automationId, timeout);
        app.ClearText(automationId);
        app.EnterText(automationId, text);
    }

    /// <summary>
    /// Liest den Text eines Elements.
    /// </summary>
    public static string GetTextFromElement(this IApp app, string automationId, TimeSpan? timeout = null)
    {
        app.WaitForAutomationId(automationId, timeout);
        var element = app.Query(automationId).FirstOrDefault();
        return element?.Text ?? string.Empty;
    }

    /// <summary>
    /// Prueft ob ein Element sichtbar ist.
    /// </summary>
    public static bool IsElementVisible(this IApp app, string automationId)
    {
        return app.Query(automationId).Any();
    }

    /// <summary>
    /// Wartet bis die App bereit ist (nach App-Start).
    /// </summary>
    public static void WaitForAppReady(this IApp app, string readyIndicatorAutomationId, TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.LongTimeout;
        app.WaitForElement(readyIndicatorAutomationId, timeout: wait);
    }

    /// <summary>
    /// Fuehrt eine Swipe-Geste aus.
    /// </summary>
    public static void SwipeLeftToRight(this IApp app)
    {
        app.SwipeLeftToRight(0.5f, 500);
    }

    /// <summary>
    /// Fuehrt eine Swipe-Geste aus.
    /// </summary>
    public static void SwipeRightToLeft(this IApp app)
    {
        app.SwipeRightToLeft(0.5f, 500);
    }

    /// <summary>
    /// Drueckt die Zurueck-Taste (Android).
    /// </summary>
    public static void GoBack(this IApp app)
    {
        if (Constants.CurrentPlatform == Platform.Android)
        {
            app.Back();
        }
        // Browser-Navigation muss ueber UI-Elemente erfolgen
    }
}
