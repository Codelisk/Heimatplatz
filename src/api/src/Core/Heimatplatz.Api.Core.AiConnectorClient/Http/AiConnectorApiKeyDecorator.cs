using Heimatplatz.Api.Core.AiConnectorClient.Configuration;
using Microsoft.Extensions.Options;
using Shiny.Mediator;
using Shiny.Mediator.Http;

namespace Heimatplatz.Api.Core.AiConnectorClient.Http;

/// <summary>
/// Fuegt den X-Api-Key Header ausschliesslich fuer generierte AiConnector-Requests hinzu,
/// damit andere ueber Shiny.Mediator generierte HTTP-Clients (z.B. das eigene Heimatplatz-API-Contract)
/// nicht versehentlich denselben Key mitschicken.
/// </summary>
public class AiConnectorApiKeyDecorator(IOptions<AiConnectorClientOptions> options) : IHttpRequestDecorator
{
    public Task Decorate(HttpRequestMessage httpRequest, IMediatorContext context, CancellationToken cancellationToken)
    {
        var apiKey = options.Value.ApiKey;
        if (!string.IsNullOrWhiteSpace(apiKey) &&
            context.Message.GetType().Namespace?.StartsWith("Heimatplatz.Api.Core.AiConnectorClient.Generated", StringComparison.Ordinal) == true)
        {
            httpRequest.Headers.Remove("X-Api-Key");
            httpRequest.Headers.Add("X-Api-Key", apiKey);
        }

        return Task.CompletedTask;
    }
}
