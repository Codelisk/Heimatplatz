using Heimatplatz.Core.UITests.PageObjects;
using Heimatplatz.UITests.Infrastructure;
using FluentAssertions;
using NUnit.Framework;

namespace Heimatplatz.Core.UITests.Tests;

/// <summary>
/// Tests fuer die MainPage-Funktionalitaet.
/// </summary>
[TestFixture]
[Category(TestCategories.Core)]
public class MainPageTests : BaseTestFixture
{
    private ShellPage _shell = null!;
    private MainPageObject _mainPage = null!;

    protected override void OnSetUp()
    {
        _shell = new ShellPage(App);
        _mainPage = new MainPageObject(App);

        // Warte bis App bereit ist
        _shell.WaitForAppReady();
        _mainPage.WaitForPage();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void MainPage_Displays_WelcomeMessage()
    {
        // Assert
        _mainPage.GetHeadlineText().Should().Contain("Welcome");
    }

    [Test]
    public void MainPage_Displays_Subtitle()
    {
        // Assert
        _mainPage.GetSubtitleText().Should().NotBeEmpty();
    }

    [Test]
    [Category(TestCategories.Smoke)]
    public void ClickButton_IncreasesCounter()
    {
        // Act
        _mainPage.ClickButton();

        // Assert
        _mainPage.IsClickCounterVisible().Should().BeTrue();
    }

    [Test]
    public void ClickButton_MultipleTimes_ShowsCorrectCount()
    {
        // Arrange
        const int clickCount = 3;

        // Act
        _mainPage.ClickButtonMultipleTimes(clickCount);

        // Assert
        var counterText = _mainPage.GetClickCountText();
        counterText.Should().Contain(clickCount.ToString());
    }

    [Test]
    public void MainPage_HasNavigationBar()
    {
        // Assert
        var title = _mainPage.GetNavigationTitle();
        title.Should().NotBeEmpty();
    }
}
