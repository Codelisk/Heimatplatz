using Heimatplatz.Http;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Shiny.Mediator.Http;

namespace Heimatplatz.Core.ApiClient.Decorators;

/// <summary>
/// Aggregates all <see cref="IHttpHeaderContributor"/> implementations and applies them to HTTP requests.
/// </summary>
/// <remarks>
/// This decorator collects all registered header contributors and executes them in priority order.
/// It serves as the single <see cref="IHttpRequestDecorator"/> that orchestrates header contribution.
/// </remarks>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
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
        foreach (var contributor in _contributors)
        {
            try
            {
                await contributor.ContributeAsync(httpMessage, ct);
                _logger.LogDebug("Applied header contributor: {ContributorType}", contributor.GetType().Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Header contributor {ContributorType} failed", contributor.GetType().Name);
                throw;
            }
        }
    }
}
