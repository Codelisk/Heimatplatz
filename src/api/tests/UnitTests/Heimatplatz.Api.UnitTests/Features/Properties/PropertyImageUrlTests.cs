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
}
