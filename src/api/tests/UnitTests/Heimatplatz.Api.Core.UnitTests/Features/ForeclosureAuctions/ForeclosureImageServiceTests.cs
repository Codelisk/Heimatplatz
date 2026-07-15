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
        var service = new ForeclosureImageService(
            Substitute.For<IHttpClientFactory>(),
            Substitute.For<IWebHostEnvironment>(),
            Options.Create(new ScrapingOptions()),
            NullLogger<ForeclosureImageService>.Instance);

        var candidates = service.ExtractPdfImageCandidates(CreatePdfWithPhoto());
        var selected = ForeclosureImageSelector.SelectPdfImages(candidates, maxCount: 20);

        selected.Should().ContainSingle();
        selected[0].PageNumber.Should().Be(2);
        selected[0].Width.Should().Be(780);
        selected[0].Height.Should().Be(520);
        selected[0].Bytes.Should().HaveCountGreaterThan(10 * 1024);
        selected[0].Extension.Should().Be(".jpg");
    }

    private static byte[] CreatePdfWithPhoto()
    {
        var builder = new PdfDocumentBuilder();
        builder.AddPage(PageSize.A4);
        var photoPage = builder.AddPage(PageSize.A4);
        photoPage.AddJpeg(CreatePhoto(), new PdfRectangle(20, 20, 560, 390));
        return builder.Build();
    }

    private static byte[] CreatePhoto()
    {
        using var bitmap = new SKBitmap(780, 520);
        var random = new Random(42);
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
}
