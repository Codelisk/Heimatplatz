using System.Collections.Concurrent;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Liefert einen vorhandenen SQLite-Cache sofort aus und aktualisiert veraltete
/// Daten in einem neuen Mediator-Scope im Hintergrund. Dadurch wartet die UI nach
/// dem ersten erfolgreichen Abruf nie auf einen Netzwerk-Roundtrip.
/// </summary>
[MiddlewareOrder(-100)]
internal sealed class LocalFirstRequestMiddleware<TRequest, TResult>(
    IConfiguration configuration,
    ICacheService cache,
    IContractKeyProvider contractKeyProvider,
    IInternetService internet,
    IMediator mediator,
    TimeProvider timeProvider,
    LocalFirstRefreshCoordinator refreshCoordinator,
    ILogger<LocalFirstRequestMiddleware<TRequest, TResult>> logger
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    private const string BackgroundRefreshHeader = "Heimatplatz.LocalFirst.BackgroundRefresh";

    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        if (context.TryGetValue<bool>(BackgroundRefreshHeader) == true)
        {
            return await next().ConfigureAwait(false);
        }

        var section = configuration.GetHandlerSection(
            "LocalFirst",
            context.Message,
            context.MessageHandler);

        if (section is null)
            return await next().ConfigureAwait(false);

        var key = contractKeyProvider.GetContractKey(context.Message);
        var entry = await cache.Get<TResult>(key, cancellationToken).ConfigureAwait(false);

        // Pull-to-Refresh fordert online bewusst frische Daten an. Wenn der Server
        // trotz allgemeiner Internetverbindung nicht erreichbar ist, bleibt der
        // vorhandene lokale Stand sichtbar.
        if (context.HasForceCacheRefresh())
        {
            if (entry is null)
                return await next().ConfigureAwait(false);

            if (!internet.IsAvailable)
                return entry.Value;

            try
            {
                return await next().ConfigureAwait(false);
            }
            catch (Exception ex) when (IsTransientServerFailure(ex, cancellationToken))
            {
                logger.LogWarning(ex, "Server nicht erreichbar; lokaler Stand fuer {RequestType} bleibt aktiv", typeof(TRequest).Name);
                return entry.Value;
            }
        }

        if (entry is null)
            return await next().ConfigureAwait(false);

        var refreshAfterSeconds = section.GetValue("RefreshAfterSeconds", 60);
        var refreshDue = refreshAfterSeconds <= 0 ||
                         timeProvider.GetUtcNow() - entry.CreatedAt >= TimeSpan.FromSeconds(refreshAfterSeconds);

        if (refreshDue && internet.IsAvailable && refreshCoordinator.TryStart(key))
        {
            var request = (TRequest)context.Message;
            Task.Run(() => RefreshAsync(request, key)).RunInBackground(logger);
        }

        logger.LogDebug("Local-first Cache-Hit fuer {RequestType}", typeof(TRequest).Name);
        return entry.Value;
    }

    private async Task RefreshAsync(TRequest request, string key)
    {
        try
        {
            await mediator.Request(
                    request,
                    CancellationToken.None,
                    refreshContext =>
                    {
                        refreshContext.AddHeader(BackgroundRefreshHeader, true);
                        refreshContext.ForceCacheRefresh();
                        refreshContext.BypassExceptionHandlingEnabled = true;
                    })
                .ConfigureAwait(false);

            logger.LogDebug("Lokale Daten fuer {RequestType} im Hintergrund aktualisiert", typeof(TRequest).Name);
        }
        finally
        {
            refreshCoordinator.Complete(key);
        }
    }

    private static bool IsTransientServerFailure(Exception exception, CancellationToken cancellationToken)
        => exception is HttpRequestException or TimeoutException ||
           (exception is TaskCanceledException && !cancellationToken.IsCancellationRequested);
}

internal sealed class LocalFirstRefreshCoordinator
{
    private readonly ConcurrentDictionary<string, byte> _running = new(StringComparer.Ordinal);

    public bool TryStart(string key) => _running.TryAdd(key, 0);

    public void Complete(string key) => _running.TryRemove(key, out _);
}
