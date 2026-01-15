using Heimatplatz.Core.UITests.AutomationIds;
using Heimatplatz.UITests.Configuration;
using Heimatplatz.UITests.PageObjects;
using Uno.UITest;

namespace Heimatplatz.Core.UITests.PageObjects;

/// <summary>
/// Page Object fuer die Shell (Haupt-Container der App).
/// </summary>
public class ShellPage : BasePage
{
    public ShellPage(IApp app) : base(app)
    {
    }

    /// <summary>
    /// Wartet bis die Shell geladen ist.
    /// </summary>
    public override void WaitForPage()
    {
        WaitForElement(CoreAutomationIds.Shell.Root);
    }

    /// <summary>
    /// Prueft ob der Splash Screen sichtbar ist.
    /// </summary>
    public bool IsSplashScreenVisible()
    {
        return ElementExists(CoreAutomationIds.Shell.SplashScreen);
    }

    /// <summary>
    /// Prueft ob der Loading-Indikator sichtbar ist.
    /// </summary>
    public bool IsLoadingVisible()
    {
        return ElementExists(CoreAutomationIds.Shell.LoadingIndicator);
    }

    /// <summary>
    /// Wartet bis der Splash Screen verschwunden ist.
    /// </summary>
    public void WaitForSplashScreenToDisappear(TimeSpan? timeout = null)
    {
        WaitForNoElement(CoreAutomationIds.Shell.SplashScreen, timeout ?? Constants.LongTimeout);
    }

    /// <summary>
    /// Wartet bis die App bereit ist (Splash Screen verschwunden, Inhalt geladen).
    /// </summary>
    public void WaitForAppReady(TimeSpan? timeout = null)
    {
        var wait = timeout ?? Constants.LongTimeout;

        // Warte bis Splash Screen weg ist
        WaitForSplashScreenToDisappear(wait);

        // Warte auf Haupt-Content (MainPage oder LoginPage)
        var foundElement = WaitForAnyElement(
            wait,
            CoreAutomationIds.MainPage.Root,
            "LoginPage.Root" // Wird in Auth Feature definiert
        );
    }

    /// <summary>
    /// Wartet auf eines von mehreren Elementen und gibt die gefundene ID zurueck.
    /// </summary>
    private string WaitForAnyElement(TimeSpan timeout, params string[] automationIds)
    {
        var startTime = DateTime.Now;

        while (DateTime.Now - startTime < timeout)
        {
            foreach (var id in automationIds)
            {
                if (ElementExists(id))
                {
                    return id;
                }
            }

            Thread.Sleep(TimeSpan.FromMilliseconds(250));
        }

        throw new TimeoutException(
            $"Keines der Elemente [{string.Join(", ", automationIds)}] wurde innerhalb von {timeout.TotalSeconds}s gefunden.");
    }
}
