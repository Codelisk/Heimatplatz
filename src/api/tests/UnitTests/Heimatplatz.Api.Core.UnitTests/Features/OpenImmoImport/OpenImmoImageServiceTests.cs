using FluentAssertions;
using Heimatplatz.Api.Features.OpenImmoImport.Configuration;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Features.OpenImmoImport.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using NUnit.Framework;
using SkiaSharp;

namespace Heimatplatz.Api.Core.UnitTests.Features.OpenImmoImport;

[TestFixture]
public class OpenImmoImageServiceTests
{
    private string _webRoot = null!;
    private IHttpClientFactory _httpClientFactory = null!;
    private OpenImmoImageService _service = null!;
    private OpenImmoFeedOptions _feed = null!;

    [SetUp]
    public void SetUp()
    {
        _webRoot = Path.Combine(Path.GetTempPath(), $"openimmo-images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_webRoot);

        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(_webRoot);

        _httpClientFactory = Substitute.For<IHttpClientFactory>();

        var options = Options.Create(new OpenImmoImportOptions());
        _service = new OpenImmoImageService(
            _httpClientFactory, environment, options, NullLogger<OpenImmoImageService>.Instance);

        _feed = new OpenImmoFeedOptions
        {
            Key = "immobaer",
            SourceName = "immobaer.at",
            SellerName = "Immobär Immobilien"
        };
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_webRoot))
            Directory.Delete(_webRoot, recursive: true);
    }

    private static byte[] CreateJpeg(int width, int height)
    {
        using var bitmap = new SKBitmap(width, height);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(SKColors.CornflowerBlue);
        using var image = SKImage.FromBitmap(bitmap);
        using var encoded = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        return encoded.ToArray();
    }

    private static OpenImmoListing CreateListing(string sourceId, params OpenImmoAttachment[] attachments)
        => new()
        {
            SourceId = sourceId,
            Type = Heimatplatz.Api.Features.Properties.Contracts.PropertyType.House,
            Title = "Test",
            Attachments = attachments.ToList()
        };

    [Test]
    public async Task Materialize_Base64Jpeg_SchreibtOriginalUndLiefertLokaleUrl()
    {
        var jpeg = CreateJpeg(40, 30);
        var listing = CreateListing("OBID-1", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(jpeg)
        });

        var urls = await _service.MaterializeAsync(_feed, listing, zip: null);

        urls.Should().ContainSingle();
        urls[0].Should().StartWith("/uploads/openimmo/immobaer/obid-1/").And.EndWith(".jpg");
        urls[0].Should().NotContain(".display.", "kleine Bilder brauchen keine Variante");

        var directory = Path.Combine(_webRoot, "uploads", "openimmo", "immobaer", "obid-1");
        Directory.GetFiles(directory).Should().HaveCount(2, "Original + manifest.json");
    }

    [Test]
    public async Task Materialize_GrossesBild_ErzeugtDisplayVariante()
    {
        var jpeg = CreateJpeg(3000, 200);
        var listing = CreateListing("OBID-2", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(jpeg)
        });

        var urls = await _service.MaterializeAsync(_feed, listing, zip: null);

        urls.Should().ContainSingle();
        urls[0].Should().EndWith(".display.jpg", "eingebettet wird die Anzeige-Variante");

        var directory = Path.Combine(_webRoot, "uploads", "openimmo", "immobaer", "obid-2");
        Directory.GetFiles(directory).Should().HaveCount(3, "Original + Display-Variante + manifest.json");
    }

    [Test]
    public async Task Materialize_ZipEntry_WirdUebernommen()
    {
        var jpeg = CreateJpeg(50, 50);
        var zip = Substitute.For<IOpenImmoZipAccessor>();
        zip.ReadEntry("bilder/haus.jpg", Arg.Any<long>()).Returns(jpeg);

        var listing = CreateListing("OBID-3", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.ZipEntry,
            Location = "bilder/haus.jpg"
        });

        var urls = await _service.MaterializeAsync(_feed, listing, zip);

        urls.Should().ContainSingle().Which.Should().EndWith(".jpg");
    }

    [Test]
    public async Task Materialize_ZweiterLauf_NutztManifestOhneNeuzuschreiben()
    {
        var jpeg = CreateJpeg(40, 30);
        var listing = CreateListing("OBID-4", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(jpeg)
        });

        var first = await _service.MaterializeAsync(_feed, listing, zip: null);
        var second = await _service.MaterializeAsync(_feed, listing, zip: null);

        second.Should().BeEquivalentTo(first);
        var directory = Path.Combine(_webRoot, "uploads", "openimmo", "immobaer", "obid-4");
        Directory.GetFiles(directory).Should().HaveCount(2, "keine Duplikate durch den zweiten Lauf");
    }

    [Test]
    public async Task Materialize_ExternOhneAllowlist_LaedtNichtsHerunter()
    {
        var listing = CreateListing("OBID-5", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.ExternalUrl,
            Location = "https://evil.example.com/bild.jpg"
        });

        var urls = await _service.MaterializeAsync(_feed, listing, zip: null);

        urls.Should().BeEmpty();
        _httpClientFactory.DidNotReceive().CreateClient(Arg.Any<string>());
    }

    [Test]
    public async Task Materialize_ZuGrossesBase64_WirdUebersprungen()
    {
        var environment = Substitute.For<IWebHostEnvironment>();
        environment.WebRootPath.Returns(_webRoot);
        var service = new OpenImmoImageService(
            _httpClientFactory,
            environment,
            Options.Create(new OpenImmoImportOptions { MaxAttachmentBytes = 10 }),
            NullLogger<OpenImmoImageService>.Instance);

        var listing = CreateListing("OBID-6", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(CreateJpeg(40, 30))
        });

        var urls = await service.MaterializeAsync(_feed, listing, zip: null);

        urls.Should().BeEmpty();
    }

    [Test]
    public async Task Materialize_VerschwundenesBild_WirdAufgeraeumt()
    {
        var jpegA = CreateJpeg(40, 30);
        var jpegB = CreateJpeg(41, 30);

        var listingWithA = CreateListing("OBID-7", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(jpegA)
        });
        var listingWithB = CreateListing("OBID-7", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(jpegB)
        });

        var firstUrls = await _service.MaterializeAsync(_feed, listingWithA, zip: null);
        var secondUrls = await _service.MaterializeAsync(_feed, listingWithB, zip: null);

        secondUrls.Should().NotBeEquivalentTo(firstUrls);
        var directory = Path.Combine(_webRoot, "uploads", "openimmo", "immobaer", "obid-7");
        var firstFileName = Path.GetFileName(firstUrls[0]);
        Directory.GetFiles(directory).Select(Path.GetFileName)
            .Should().NotContain(firstFileName, "das alte Bild ist nicht mehr im Feed");
    }

    [Test]
    public async Task Materialize_OhneAnhaenge_LoeschtAltbestand()
    {
        var jpeg = CreateJpeg(40, 30);
        var listingWithImage = CreateListing("OBID-8", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(jpeg)
        });
        await _service.MaterializeAsync(_feed, listingWithImage, zip: null);

        var urls = await _service.MaterializeAsync(_feed, CreateListing("OBID-8"), zip: null);

        urls.Should().BeEmpty();
        Directory.Exists(Path.Combine(_webRoot, "uploads", "openimmo", "immobaer", "obid-8"))
            .Should().BeFalse();
    }

    [Test]
    public async Task DeleteListingImages_EntferntOrdner()
    {
        var listing = CreateListing("OBID-9", new OpenImmoAttachment
        {
            Mode = OpenImmoAttachmentMode.Base64,
            Base64Content = Convert.ToBase64String(CreateJpeg(40, 30))
        });
        await _service.MaterializeAsync(_feed, listing, zip: null);

        _service.DeleteListingImages("immobaer", "OBID-9");

        Directory.Exists(Path.Combine(_webRoot, "uploads", "openimmo", "immobaer", "obid-9"))
            .Should().BeFalse();
    }

    [Test]
    public void BuildSafeSourceId_FiltertUndHasht()
    {
        OpenImmoImageService.BuildSafeSourceId("OBID-001").Should().Be("obid-001");
        OpenImmoImageService.BuildSafeSourceId("AB/1:2#x")
            .Should().MatchRegex("^ab12x-[0-9a-f]{8}$", "Sonderzeichen werden gefiltert und gehasht");
        OpenImmoImageService.BuildSafeSourceId("///").Should().MatchRegex("^[0-9a-f]{8}$");
    }

    [Test]
    public void DetectImageExtension_ErkenntMagicBytes()
    {
        OpenImmoImageService.DetectImageExtension(CreateJpeg(10, 10)).Should().Be(".jpg");
        OpenImmoImageService.DetectImageExtension([0x89, 0x50, 0x4e, 0x47, 0x0d]).Should().Be(".png");
        OpenImmoImageService.DetectImageExtension("RIFFxxxxWEBPvp8 "u8.ToArray()).Should().Be(".webp");
        OpenImmoImageService.DetectImageExtension("kein bild"u8.ToArray()).Should().BeNull();
    }
}
