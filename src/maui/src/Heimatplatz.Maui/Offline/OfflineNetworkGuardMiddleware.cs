using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Letzte Schranke vor den generierten HTTP-Handlern. Offline-faehige Reads
/// wurden davor bereits aus Cache/Offline-Store beantwortet; alle uebrigen
/// Serveroperationen werden ohne Netzwerkversuch abgebrochen.
/// </summary>
internal sealed class OfflineNetworkGuardMiddleware<TRequest, TResult>(
    IInternetService internet
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    private const string GeneratedNamespace = "Heimatplatz.Maui.ApiClient.Generated";

    public Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (internet.IsAvailable || context.Message.GetType().Namespace != GeneratedNamespace)
            return next();

        throw new HttpRequestException(
            "Diese Aktion benötigt eine Internetverbindung. Lokal gespeicherte Daten bleiben verfügbar.");
    }
}
