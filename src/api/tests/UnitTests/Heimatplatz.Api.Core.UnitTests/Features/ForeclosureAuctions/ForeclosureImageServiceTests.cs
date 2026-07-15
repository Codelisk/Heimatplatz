using FluentAssertions;
using Heimatplatz.Api.Features.ForeclosureAuctions.Configuration;
using Heimatplatz.Api.Features.ForeclosureAuctions.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SkiaSharp;
using UglyToad.PdfPig.Content;
using UglyToad.PdfPig.Core;
using UglyToad.PdfPig.Writer;

namespace Heimatplatz.Api.Core.UnitTests.Features.ForeclosureAuctions;

[TestFixture]
public class ForeclosureImageServiceTests
{
    [Test]
    public void ExtractPdfImageCandidates_ReadsEmbeddedPhotoAtOriginalResolution()
    {
        var service = CreateService();

        var candidates = service.ExtractPdfImageCandidates(CreatePdfWithPhoto());
        var selected = ForeclosureImageSelector.SelectPdfImages(candidates, maxCount: 20);

        selected.Should().ContainSingle();
        selected[0].PageNumber.Should().Be(2);
        selected[0].Width.Should().Be(780);
        selected[0].Height.Should().Be(520);
        selected[0].Bytes.Should().HaveCountGreaterThan(10 * 1024);
        selected[0].Extension.Should().Be(".jpg");
    }

    [Test]
    public void ExtractPdfImageCandidates_SortsImagesInVisualReadingOrder()
    {
        var bottomPhoto = CreatePhoto(seed: 41);
        var topPhoto = CreatePhoto(seed: 42);
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        var page = builder.AddPage(PageSize.A4);
        page.AddJpeg(bottomPhoto, new PdfRectangle(20, 20, 560, 390));
        page.AddJpeg(topPhoto, new PdfRectangle(20, 430, 560, 800));

        var candidates = CreateService().ExtractPdfImageCandidates(builder.Build());

        candidates.Should().HaveCount(2);
        candidates[0].Bytes.Should().Equal(topPhoto);
        candidates[1].Bytes.Should().Equal(bottomPhoto);
    }

    [Test]
    public void ApplyPdfRotation_RotatesPositiveAngleCounterClockwise()
    {
        var sourceBytes = CreateOrientationPng();

        var result = ForeclosureImageService.ApplyPdfRotation(
            sourceBytes,
            ".png",
            width: 4,
            height: 2,
            rotationDegrees: 90);

        result.Width.Should().Be(2);
        result.Height.Should().Be(4);
        using var rotated = SKBitmap.Decode(result.Bytes);
        rotated.GetPixel(0, 0).Should().Be(SKColors.Green);
        rotated.GetPixel(1, 0).Should().Be(SKColors.Yellow);
        rotated.GetPixel(0, 3).Should().Be(SKColors.Red);
        rotated.GetPixel(1, 3).Should().Be(SKColors.Blue);
    }

    private static ForeclosureImageService CreateService() => new(
        Substitute.For<IHttpClientFactory>(),
        Substitute.For<IWebHostEnvironment>(),
        Options.Create(new ScrapingOptions()),
        NullLogger<ForeclosureImageService>.Instance);

    private static byte[] CreatePdfWithPhoto()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        var photoPage = builder.AddPage(PageSize.A4);
        photoPage.AddJpeg(CreatePhoto(), new PdfRectangle(20, 20, 560, 390));
        return builder.Build();
    }

    private static byte[] CreatePhoto(int seed = 42)
    {
        using var bitmap = new SKBitmap(780, 520);
        var random = new Random(seed);
        for (var y = 0; y < bitmap.Height; y++)
        {
            for (var x = 0; x < bitmap.Width; x++)
            {
                bitmap.SetPixel(x, y, new SKColor(
                    (byte)((x + random.Next(32)) % 256),
                    (byte)((y + random.Next(32)) % 256),
                    (byte)((x + y + random.Next(32)) % 256)));
            }
        }

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return data.ToArray();
    }

    private static byte[] CreateOrientationPng()
    {
        using var bitmap = new SKBitmap(4, 2);
        bitmap.SetPixel(0, 0, SKColors.Red);
        bitmap.SetPixel(1, 0, SKColors.Red);
        bitmap.SetPixel(2, 0, SKColors.Green);
        bitmap.SetPixel(3, 0, SKColors.Green);
        bitmap.SetPixel(0, 1, SKColors.Blue);
        bitmap.SetPixel(1, 1, SKColors.Blue);
        bitmap.SetPixel(2, 1, SKColors.Yellow);
        bitmap.SetPixel(3, 1, SKColors.Yellow);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }
}
