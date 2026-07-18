using FluentAssertions;
using Heimatplatz.Api.Shared.Media;
using NUnit.Framework;
using SkiaSharp;

namespace Heimatplatz.Api.Core.UnitTests.Shared;

[TestFixture]
public class ImageDisplayVariantTests
{
    [Test]
    public void TryCreate_DownscalesLargePhotoToMaxDimension()
    {
        var original = CreateJpeg(4000, 3000);

        var display = ImageDisplayVariant.TryCreate(original);

        display.Should().NotBeNull();
        using var decoded = SKBitmap.Decode(display);
        // JPEG skaliert nativ in 1/8-Schritten - die laengste Kante darf das Limit
        // nie ueberschreiten, landet aber nicht zwingend exakt darauf (4000*5/8=2500)
        decoded.Width.Should().BeLessThanOrEqualTo(ImageDisplayVariant.MaxDimension)
            .And.BeGreaterThan(ImageDisplayVariant.MaxDimension / 2);
        // Seitenverhaeltnis 4:3 bleibt erhalten
        decoded.Height.Should().Be(decoded.Width * 3 / 4);
    }

    [Test]
    public void TryCreate_DownscalesPortraitAlongLongestEdge()
    {
        var original = CreateJpeg(3000, 4000);

        var display = ImageDisplayVariant.TryCreate(original);

        display.Should().NotBeNull();
        using var decoded = SKBitmap.Decode(display);
        decoded.Height.Should().BeLessThanOrEqualTo(ImageDisplayVariant.MaxDimension)
            .And.BeGreaterThan(ImageDisplayVariant.MaxDimension / 2);
        decoded.Width.Should().Be(decoded.Height * 3 / 4);
    }

    [Test]
    public void TryCreate_ReturnsNullForSmallCorrectlyOrientedPhoto()
    {
        var original = CreateJpeg(1200, 800);

        ImageDisplayVariant.TryCreate(original).Should().BeNull();
    }

    [Test]
    public void TryCreate_ReturnsNullForUndecodableData()
    {
        ImageDisplayVariant.TryCreate([0x00, 0x01, 0x02, 0x03]).Should().BeNull();
    }

    [Test]
    public void GetDisplayFileName_AppendsSuffixBeforeJpegExtension()
    {
        ImageDisplayVariant.GetDisplayFileName("3f2a.png").Should().Be("3f2a.display.jpg");
        ImageDisplayVariant.GetDisplayFileName("3f2a.jpg").Should().Be("3f2a.display.jpg");
    }

    [Test]
    public void GetFileStem_StripsSuffixAndExtensionFromBothVariants()
    {
        ImageDisplayVariant.GetFileStem("3f2a.display.jpg").Should().Be("3f2a");
        ImageDisplayVariant.GetFileStem("3f2a.jpg").Should().Be("3f2a");
        ImageDisplayVariant.GetFileStem("3f2a.webp").Should().Be("3f2a");
    }

    private static byte[] CreateJpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.SteelBlue);
        using var paint = new SKPaint { Color = SKColors.OrangeRed };
        canvas.DrawRect(width / 4f, height / 4f, width / 2f, height / 2f, paint);

        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 90);
        return encoded.ToArray();
    }
}
