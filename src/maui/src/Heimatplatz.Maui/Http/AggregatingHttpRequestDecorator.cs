using System.Web;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;
using Shiny.Mediator.Http;

namespace Heimatplatz.Maui.Http;

/// <summary>
/// Aggregates all <see cref="IHttpHeaderContributor"/> implementations and applies them to HTTP requests.
/// Also fixes DateTimeOffset query parameter serialization for Shiny.Mediator's source generator.
/// </summary>
/// <remarks>
/// This decorator collects all registered header contributors and executes them in priority order.
/// It serves as the single <see cref="IHttpRequestDecorator"/> that orchestrates header contribution.
/// Additionally, it fixes the CreatedAfter parameter which Shiny.Mediator's source generator
/// serializes using ToString() instead of ISO 8601 format.
/// </remarks>
[Singleton]
public sealed class AggregatingHttpRequestDecorator : IHttpRequestDecorator
{
    private readonly IReadOnlyList<IHttpHeaderContributor> _contributors;
    private readonly ILogger<AggregatingHttpRequestDecorator> _logger;

    public AggregatingHttpRequestDecorator(
        IEnumerable<IHttpHeaderContributor> contributors,
        ILogger<AggregatingHttpRequestDecorator> logger)
    {
        _contributors = contributors.OrderBy(c => c.Priority).ToList();
        _logger = logger;
    }

    public async Task Decorate(HttpRequestMessage httpMessage, IMediatorContext context, CancellationToken ct)
    {
        FixDateTimeQueryParameters(httpMessage);

        foreach (var contributor in _contributors)
        {
            try
            {
                await contributor.ContributeAsync(httpMessage, ct).ConfigureAwait(false);
                _logger.LogDebug("Applied header contributor: {ContributorType}", contributor.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Header contributor {ContributorType} failed", contributor.GetType().Name);
                throw;
            }
        }
    }

    /// <summary>
    /// Fixes DateTimeOffset query parameters that were serialized incorrectly by the source generator.
    /// The Shiny.Mediator source generator uses ToString() for query parameters, which doesn't
    /// produce proper ISO 8601 format required by the API.
    /// </summary>
    /// <remarks>
    /// The source generator produces formats like "01/09/2026 10:47:19 +01:00".
    /// In URLs, the "+" is decoded as a space by HttpUtility.ParseQueryString,
    /// resulting in "01/09/2026 10:47:19  01:00" which fails to parse.
    /// This method handles that case by restoring the "+" before parsing.
    /// </remarks>
    private void FixDateTimeQueryParameters(HttpRequestMessage httpMessage)
    {
        if (httpMessage.RequestUri?.Query == null || !httpMessage.RequestUri.Query.Contains("CreatedAfter="))
            return;

        var uri = httpMessage.RequestUri;
        var query = HttpUtility.ParseQueryString(uri.Query);
        var createdAfter = query["CreatedAfter"];

        if (string.IsNullOrEmpty(createdAfter))
            return;

        var fixedValue = FixTimezoneOffset(createdAfter);

        if (DateTimeOffset.TryParse(fixedValue, out var dateTimeOffset))
        {
            var isoFormat = dateTimeOffset.ToUniversalTime().ToString("o");
            query["CreatedAfter"] = isoFormat;

            var uriBuilder = new UriBuilder(uri)
            {
                Query = query.ToString()
            };
            httpMessage.RequestUri = uriBuilder.Uri;

            _logger.LogInformation("[DateTimeFix] Fixed CreatedAfter: {Original} -> {Fixed}", createdAfter, isoFormat);
        }
        else
        {
            _logger.LogWarning("[DateTimeFix] Could not parse CreatedAfter value: {Value} (fixed attempt: {Fixed})", createdAfter, fixedValue);
        }
    }

    /// <summary>
    /// Fixes timezone offset that was corrupted by URL decoding (+ became space).
    /// Converts patterns like "2026-01-09 10:47:19  01:00" back to "2026-01-09 10:47:19 +01:00"
    /// </summary>
    private static string FixTimezoneOffset(string value)
    {
        if (value.Length < 6)
            return value;

        var lastPart = value[^6..];
        if (lastPart.Length == 6 &&
            lastPart[0] == ' ' &&
            char.IsDigit(lastPart[1]) &&
            char.IsDigit(lastPart[2]) &&
            lastPart[3] == ':' &&
            char.IsDigit(lastPart[4]) &&
            char.IsDigit(lastPart[5]))
        {
            return value[..^6] + "+" + value[^5..];
        }

        return value;
    }
}
