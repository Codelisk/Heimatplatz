using Shiny.Mediator;

namespace Heimatplatz.Api.Core.AiConnectorClient.Generated;

/// <summary>
/// Erweitert den generierten <c>RunPromptHttpRequestHandler</c> (partial class) um einen
/// festen 5-Minuten-Timeout NUR fuer diesen Handler. Shiny.Mediator's
/// BaseHttpRequestHandler.HandleRequest liest sonst den globalen (nicht Namespace-
/// spezifischen) Default-Timeout aus "Mediator:Http:Timeout" (Framework-Default 20s) -
/// zu knapp fuer die Claude-CLI-Ausfuehrung am AiConnector-Server (Warteschlange bei
/// gleichzeitigen Anfragen, KI-Generierung). Andere/zukuenftige generierte HTTP-Clients
/// bleiben unangetastet beim 20s-Default, da hier gezielt nur diese eine Handler-Klasse
/// erweitert wird statt der globale Config-Key veraendert zu werden.
/// </summary>
public partial class RunPromptHttpRequestHandler
{
    private static readonly TimeSpan AiConnectorTimeout = TimeSpan.FromMinutes(5);

    protected override async Task<TResult> HandleRequest<TRequest, TResult>(
        HttpRequestMessage httpRequest,
        TRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        await this.Decorate(context, httpRequest, cancellationToken).ConfigureAwait(false);
        return await this.Send<TResult>(context, httpRequest, AiConnectorTimeout, cancellationToken).ConfigureAwait(false);
    }
}
