using System.Net;
using FluentAssertions;
using Heimatplatz.Api.IntegrationTests.Infrastructure;
using NUnit.Framework;

namespace Heimatplatz.Api.IntegrationTests.Endpoints;

/// <summary>
/// Tests fuer die StammdatenConditionalGetMiddleware: Stammdaten-Routen liefern
/// einen Content-Hash-ETag und beantworten If-None-Match mit einem koerperlosen 304.
/// </summary>
[TestFixture]
[Category(TestCategories.Endpoint)]
[Category(TestCategories.Integration)]
public class StammdatenConditionalGetTests : BaseApiIntegrationTest
{
    [TestCase("/api/legal/imprint")]
    [TestCase("/api/legal/privacy-policy")]
    [TestCase("/api/legal/contact")]
    [TestCase("/api/locations/")]
    public async Task StammdatenRoute_ReturnsETagAndNoCacheHeader(string path)
    {
        var response = await Client.GetAsync(path);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.IsWeak.Should().BeFalse();
        response.Headers.CacheControl!.NoCache.Should().BeTrue();
    }

    [TestCase("/api/legal/imprint")]
    [TestCase("/api/locations/")]
    public async Task StammdatenRoute_MatchingIfNoneMatch_Returns304WithoutBody(string path)
    {
        var first = await Client.GetAsync(path);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var etag = first.Headers.ETag!.Tag;
        var body = await first.Content.ReadAsStringAsync();
        body.Should().NotBeNullOrEmpty();

        using var conditional = new HttpRequestMessage(HttpMethod.Get, path);
        conditional.Headers.TryAddWithoutValidation("If-None-Match", etag);
        var second = await Client.SendAsync(conditional);

        second.StatusCode.Should().Be(HttpStatusCode.NotModified);
        second.Headers.ETag!.Tag.Should().Be(etag);
        (await second.Content.ReadAsStringAsync()).Should().BeEmpty();
    }

    [Test]
    public async Task StammdatenRoute_StaleIfNoneMatch_ReturnsFullResponse()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/legal/imprint");
        request.Headers.TryAddWithoutValidation("If-None-Match", "\"veralteter-etag\"");
        var response = await Client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().NotBeNullOrEmpty();
    }

    [Test]
    public async Task StammdatenRoute_RepeatedRequests_ReturnStableETag()
    {
        var first = await Client.GetAsync("/api/legal/imprint");
        var second = await Client.GetAsync("/api/legal/imprint");

        first.Headers.ETag!.Tag.Should().Be(second.Headers.ETag!.Tag);
    }

    [Test]
    public async Task NonStammdatenRoute_GetsNoConditionalGetETag()
    {
        var response = await Client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Headers.ETag.Should().BeNull();
    }
}
