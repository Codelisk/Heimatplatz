using FluentAssertions;
using Heimatplatz.Api.Features.Properties.Handlers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Properties;

[TestFixture]
public class PropertyImageUrlTests
{
    [Test]
    public void ResolveApiBaseUrl_UsesAndroidRequestOriginInsteadOfConfiguredLoopback()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("10.0.2.2", 5292);
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:PublicBaseUrl"] = "http://localhost:5292"
            })
            .Build();

        var result = GetPropertiesHandler.ResolveApiBaseUrl(accessor, configuration);

        result.Should().Be("http://10.0.2.2:5292");
    }

    [Test]
    public void ResolveApiBaseUrl_KeepsConfiguredPublicOrigin()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("api", 8080);
        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:PublicBaseUrl"] = "https://api.heimatplatz.at/"
            })
            .Build();

        var result = GetPropertiesHandler.ResolveApiBaseUrl(accessor, configuration);

        result.Should().Be("https://api.heimatplatz.at");
    }

    [Test]
    public void ProxyImageUrls_RebasesLoopbackSeedUrlToRequestOrigin()
    {
        var result = GetPropertiesHandler.ProxyImageUrls(
            ["http://localhost:5292/seed/house.jpg?variant=1"],
            "http://10.0.2.2:5292",
            width: 640);

        result.Should().ContainSingle()
            .Which.Should().Be("http://10.0.2.2:5292/seed/house.jpg?variant=1");
    }

    [Test]
    public void ProxyImageUrls_RebasesAndResizesLoopbackUploadUrl()
    {
        var result = GetPropertiesHandler.ProxyImageUrls(
            ["http://localhost:5292/uploads/property/photo.jpg"],
            "http://10.0.2.2:5292",
            width: 640);

        result.Should().ContainSingle()
            .Which.Should().Be(
                "http://10.0.2.2:5292/api/images/local?path=%2Fuploads%2Fproperty%2Fphoto.jpg&w=640");
    }

    [Test]
    public void ProxyImageUrls_KeepsSameOriginSeedUrl()
    {
        const string url = "http://localhost:5292/seed/house.jpg";

        var result = GetPropertiesHandler.ProxyImageUrls([url], "http://localhost:5292");

        result.Should().ContainSingle().Which.Should().Be(url);
    }

    /// <summary>
    /// Die Detailseite zeigt die Vorschau-Breite, nicht die bis zu 2560px breite
    /// Display-Variante - sonst laedt jedes Foto ein Vielfaches der noetigen Pixel.
    /// </summary>
    [Test]
    public void ProxyImageUrls_UsesDetailPreviewWidthForUploads()
    {
        var result = GetPropertiesHandler.ProxyImageUrls(
            ["http://localhost:5292/uploads/property/photo.jpg"],
            "http://localhost:5292",
            width: GetPropertyByIdHandler.DetailPreviewWidth);

        result.Should().ContainSingle()
            .Which.Should().Be(
                "http://localhost:5292/api/images/local?path=%2Fuploads%2Fproperty%2Fphoto.jpg&w=1280");
    }

    /// <summary>
    /// Das Thumbnail der Detailantwort muss zeichengleich mit der Listen-URL sein -
    /// nur dann findet der Bild-Cache der App das von der Karte geladene Foto wieder
    /// und kann es sofort als Platzhalter zeigen.
    /// </summary>
    [Test]
    public void ProxyImageUrls_DetailThumbnailMatchesListUrlExactly()
    {
        List<string> source = ["http://localhost:5292/uploads/property/photo.jpg"];

        var listUrl = GetPropertiesHandler.ProxyImageUrls(
            source, "http://localhost:5292", width: GetPropertiesHandler.ListThumbnailWidth);
        var detailThumbnail = GetPropertiesHandler.ProxyImageUrls(
            source, "http://localhost:5292", width: GetPropertiesHandler.ListThumbnailWidth);

        detailThumbnail.Should().Equal(listUrl);
    }

    /// <summary>
    /// Externe Bilder (ZV-Sync) laufen ueber den Proxy - auch dort muss die
    /// Vorschau-Breite ankommen.
    /// </summary>
    [Test]
    public void ProxyImageUrls_AppliesWidthToExternalProxiedImages()
    {
        var result = GetPropertiesHandler.ProxyImageUrls(
            ["https://edikte.example.at/bild.jpg"],
            "https://api.heimatplatz.at",
            width: GetPropertyByIdHandler.DetailPreviewWidth);

        result.Should().ContainSingle()
            .Which.Should().EndWith("&w=1280")
            .And.StartWith("https://api.heimatplatz.at/api/images/proxy?url=");
    }
}
