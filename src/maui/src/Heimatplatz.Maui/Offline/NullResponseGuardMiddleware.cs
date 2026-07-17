using Microsoft.Extensions.Configuration;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui.Offline;

/// <summary>
/// Sitzt zwischen Cache- und Offline-Middleware. Die Offline-Middleware liefert ohne
/// Internet UND ohne gespeicherte Antwort null zurueck - dieses null wuerde die
/// Cache-Middleware dauerhaft persistieren (AbsoluteExpiration=0) und die UI wuerde
/// es als "nicht mehr verfuegbar" fehlinterpretieren. Fuer Offline-faehige Reads ist
/// null nie ein gueltiges Ergebnis, daher wird stattdessen eine Exception geworfen.
/// </summary>
internal sealed class NullResponseGuardMiddleware<TRequest, TResult>(
    IConfiguration configuration
) : IRequestMiddleware<TRequest, TResult>
    where TRequest : IRequest<TResult>
{
    public async Task<TResult> Process(
        IMediatorContext context,
        RequestHandlerDelegate<TResult> next,
        CancellationToken cancellationToken)
    {
        var result = await next().ConfigureAwait(false);

        if (result is null && IsOfflineEnabled(context))
            throw new OfflineDataUnavailableException();

        return result;
    }

    private bool IsOfflineEnabled(IMediatorContext context)
    {
        var section = configuration.GetHandlerSection(
            "Offline",
            context.Message,
            context.MessageHandler);

        return section is not null && bool.TryParse(section.Value, out var enabled) && enabled;
    }
}
