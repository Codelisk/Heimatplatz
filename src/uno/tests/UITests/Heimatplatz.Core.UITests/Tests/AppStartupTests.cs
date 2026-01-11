using Heimatplatz.Core.UITests.PageObjects;
using Heimatplatz.UITests.Configuration;
using Heimatplatz.UITests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Heimatplatz.Core.UITests.Tests;

/// <summary>
/// Tests fuer den App-Start und Initialisierung.
/// </summary>
[TestFixture]
[Category(TestCategories.Smoke)]
[Category(TestCategories.Core)]
public class AppStartupTests : BaseTestFixture
{
    private ShellPage _shell = null!;

    protected override void OnSetUp()
    {
        _shell = new ShellPage(App);
    }

    [Test]
    [Category(TestCategories.Critical)]
    public void App_Starts_Successfully()
    {
        // Act & Assert
        _shell.WaitForAppReady();
    }

    [Test]
    public void SplashScreen_Disappears_After_Loading()
    {
        // Assert - Splash Screen sollte nach dem Laden verschwinden
        _shell.WaitForSplashScreenToDisappear();
        _shell.IsSplashScreenVisible().Should().BeFalse();
    }

    [Test]
    public void App_Shows_MainContent_After_Start()
    {
        // Arrange
        _shell.WaitForAppReady();

        // Assert - Nach dem Start sollte entweder MainPage oder LoginPage sichtbar sein
        var mainPage = new MainPageObject(App);
        var isMainPageVisible = mainPage.IsDisplayed();

        // Je nach Auth-Status wird MainPage oder LoginPage angezeigt
        isMainPageVisible.Should().BeTrue(
            "MainPage sollte nach dem App-Start sichtbar sein (oder LoginPage bei aktivierter Auth)");
    }
}
