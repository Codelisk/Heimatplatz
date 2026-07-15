using FluentAssertions;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using NUnit.Framework;

namespace Heimatplatz.Api.Core.UnitTests.Features.ForeclosureAuctions;

[TestFixture]
public class ForeclosureImageSelectorTests
{
    [Test]
    public void SelectDirectImages_FiltersTinyPlaceholdersAndPlans()
    {
        var candidates = new[]
        {
            Image("https://edikte.justiz.gv.at/$file/siehe%20Beilagen.jpg", 300, 186, "Foto (5 KB)"),
            Image("https://edikte.justiz.gv.at/$file/Lageplan.jpg", 2480, 3507, "Lageplan"),
            Image("https://edikte.justiz.gv.at/$file/Aussenansichten%201.jpg", 225, 302, "Außenansichten"),
            Image("https://edikte.justiz.gv.at/$file/Wohnhaus.jpg", 1200, 800, "Wohnhaus von Süden")
        };

        var result = ForeclosureImageSelector.SelectDirectImages(candidates);

        result.Should().ContainSingle()
            .Which.Url.Should().EndWith("/Wohnhaus.jpg");
    }

    [Test]
    public void SelectDirectImages_PrefersHouseOverLargerSecondaryBuilding()
    {
        var candidates = new[]
        {
            Image("https://edikte.justiz.gv.at/$file/Bootshuette.jpg", 2736, 1824, "Bootshütte", 0),
            Image("https://edikte.justiz.gv.at/$file/Wohnhaus.jpg", 1200, 800, "Wohnhaus von Süden", 1)
        };

        var result = ForeclosureImageSelector.SelectDirectImages(candidates);

        result.Select(image => image.Url).Should().Equal(
            "https://edikte.justiz.gv.at/$file/Wohnhaus.jpg",
            "https://edikte.justiz.gv.at/$file/Bootshuette.jpg");
    }

    [Test]
    public void SelectDirectImages_AllowsModerateImageOnlyAsLastResort()
    {
        var candidate = Image("https://edikte.justiz.gv.at/$file/Fotos.jpg", 520, 340, "Wohnhaus");

        ForeclosureImageSelector.SelectDirectImages([candidate]).Should().BeEmpty();
        ForeclosureImageSelector.SelectDirectImages([candidate], requirePrimaryQuality: false)
            .Should().ContainSingle();
    }

    [Test]
    public void SelectPdfImages_PrefersPhotoPagesAndRejectsPlans()
    {
        var bytes = new byte[20 * 1024];
        var candidates = new[]
        {
            new PdfImageCandidate(1, 0, "Titel", 1600, 1000, bytes, ".jpg"),
            new PdfImageCandidate(20, 0, "Lageplan", 1600, 1000, bytes, ".jpg"),
            new PdfImageCandidate(24, 0, "Fotos Wohnhaus", 780, 520, bytes, ".jpg"),
            new PdfImageCandidate(25, 0, "Wohnhaus Erdgeschoss", 780, 520, bytes, ".jpg")
        };

        var result = ForeclosureImageSelector.SelectPdfImages(candidates, maxCount: 20);

        result.Select(image => image.PageNumber).Should().Equal(24, 25);
    }

    private static EdiktImageCandidate Image(
        string url,
        int width,
        int height,
        string title,
        int order = 0) => new()
        {
            Url = url,
            Width = width,
            Height = height,
            Title = title,
            AltText = title,
            IsPhoto = true,
            DocumentOrder = order
        };
}
