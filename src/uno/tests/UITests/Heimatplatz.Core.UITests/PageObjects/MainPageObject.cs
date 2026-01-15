using Heimatplatz.Core.UITests.AutomationIds;
using Heimatplatz.UITests.Configuration;
using Heimatplatz.UITests.PageObjects;
using Uno.UITest;

namespace Heimatplatz.Core.UITests.PageObjects;

/// <summary>
/// Page Object fuer die MainPage.
/// </summary>
public class MainPageObject : BasePage
{
    public MainPageObject(IApp app) : base(app)
    {
    }

    /// <summary>
    /// Wartet bis die MainPage geladen ist.
    /// </summary>
    public override void WaitForPage()
    {
        WaitForElement(CoreAutomationIds.MainPage.Root);
    }

    /// <summary>
    /// Prueft ob die MainPage sichtbar ist.
    /// </summary>
    public bool IsDisplayed()
    {
        return ElementExists(CoreAutomationIds.MainPage.Root);
    }

    /// <summary>
    /// Gibt den Headline-Text zurueck.
    /// </summary>
    public string GetHeadlineText()
    {
        return GetText(CoreAutomationIds.MainPage.HeadlineText);
    }

    /// <summary>
    /// Gibt den Subtitle-Text zurueck.
    /// </summary>
    public string GetSubtitleText()
    {
        return GetText(CoreAutomationIds.MainPage.SubtitleText);
    }

    /// <summary>
    /// Gibt den aktuellen Click-Count zurueck.
    /// </summary>
    public string GetClickCountText()
    {
        if (!ElementExists(CoreAutomationIds.MainPage.ClickCountText))
        {
            return string.Empty;
        }

        return GetText(CoreAutomationIds.MainPage.ClickCountText);
    }

    /// <summary>
    /// Klickt auf den "Click Me" Button.
    /// </summary>
    public void ClickButton()
    {
        Tap(CoreAutomationIds.MainPage.ClickButton);
    }

    /// <summary>
    /// Klickt mehrfach auf den Button.
    /// </summary>
    public void ClickButtonMultipleTimes(int count)
    {
        for (var i = 0; i < count; i++)
        {
            ClickButton();
            Thread.Sleep(100); // Kurze Pause zwischen Klicks
        }
    }

    /// <summary>
    /// Prueft ob der Click-Counter sichtbar ist.
    /// </summary>
    public bool IsClickCounterVisible()
    {
        return ElementExists(CoreAutomationIds.MainPage.ClickCountText);
    }

    /// <summary>
    /// Prueft ob der Busy-Overlay sichtbar ist.
    /// </summary>
    public bool IsBusy()
    {
        return ElementExists(CoreAutomationIds.MainPage.BusyOverlay);
    }

    /// <summary>
    /// Wartet bis der Busy-Overlay verschwunden ist.
    /// </summary>
    public void WaitForNotBusy(TimeSpan? timeout = null)
    {
        WaitForNoElement(CoreAutomationIds.MainPage.BusyOverlay, timeout ?? Constants.DefaultTimeout);
    }

    /// <summary>
    /// Gibt den Title aus der NavigationBar zurueck.
    /// </summary>
    public string GetNavigationTitle()
    {
        return GetText(CoreAutomationIds.MainPage.NavigationBar);
    }
}
