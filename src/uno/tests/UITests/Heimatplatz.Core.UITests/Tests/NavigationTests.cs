using Heimatplatz.Core.UITests.PageObjects;
using Heimatplatz.UITests.Configuration;
using Heimatplatz.UITests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Heimatplatz.Core.UITests.Tests;

/// <summary>
/// Tests fuer die Navigation.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
[Category(TestCategories.Navigation)]
public class NavigationTests : BaseTestFixture
{
    private ShellPage _shell = null!;
    private MainPageObject _mainPage = null!;

    protected override void OnSetUp()
    {
        _shell = new ShellPage(App);
        _mainPage = new MainPageObject(App);

        // Warte bis App bereit ist
        _shell.WaitForAppReady();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void InitialPage_IsMainPage()
    {
        // Assert
        _mainPage.IsDisplayed().Should().BeTrue();
    }

    [Test]
    [Category(TestCategories.Android)]
    public void BackButton_DoesNotCrashApp_OnMainPage()
    {
        // Arrange
        _mainPage.WaitForPage();

        // Act - Zurueck-Taste druecken (nur Android)
        if (CurrentPlatform == Platform.Android)
        {
            App.Back();
            Thread.Sleep(500); // Kurze Pause

            // Assert - App sollte nicht gecrasht sein
            // Bei MainPage sollte entweder die App beendet werden oder nichts passieren
            // Dies haengt von der App-Konfiguration ab
        }
    }

    [Test]
    public void NavigationBar_IsVisible()
    {
        // Arrange
        _mainPage.WaitForPage();

        // Assert
        var title = _mainPage.GetNavigationTitle();
        title.Should().NotBeEmpty("NavigationBar sollte einen Titel haben");
    }
}
