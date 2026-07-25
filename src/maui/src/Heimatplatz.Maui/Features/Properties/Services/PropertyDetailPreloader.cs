using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Core.Media;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Wird beim Antippen einer Karte aufgerufen - noch bevor navigiert wird.
///
/// Zwei Dinge passieren dabei: die bereits vorhandenen Listendaten werden fuer die
/// Detailseite abgelegt (ihr Kopf steht damit sofort), und der Detail-Request startet
/// bereits waehrend Seitenaufbau und Navigationsanimation laufen. Die Detailseite holt
/// sich diesen laufenden Request ueber <see cref="TryTakePendingRequest"/> ab, statt
/// einen zweiten zu stellen.
/// </summary>
[Singleton]
public sealed class PropertyDetailPreloader(
    PropertyHandoffCache handoffCache,
    PropertyImageCache imageCache,
    DetailNavigationTrace trace,
    IMediator mediator,
    ILogger<PropertyDetailPreloader> logger)
{
    private readonly object _gate = new();

    // Nur der zuletzt angetippte Eintrag: die Detailseite holt ihn unmittelbar nach der
    // Navigation ab. Aeltere Prefetches haben ihren Zweck erfuellt (der Cache ist warm).
    private Guid _pendingId;
    private Task<GetPropertyByIdResponse?>? _pendingRequest;

    public void Prepare(PropertyListItemDto property)
    {
        trace.Start(property.Id);
        handoffCache.Put(property);
        StartRequest(property.Id);
    }

    /// <summary>
    /// Uebergibt den laufenden Detail-Request an die Detailseite. Liefert null, wenn
    /// kein passender Prefetch existiert (Deep-Link, Zurueck-Navigation, erneuter
    /// Ladeversuch) - dann stellt die Seite den Request wie bisher selbst.
    /// </summary>
    public Task<GetPropertyByIdResponse?>? TryTakePendingRequest(Guid propertyId)
    {
        lock (_gate)
        {
            if (_pendingRequest == null || _pendingId != propertyId)
                return null;

            var request = _pendingRequest;
            _pendingRequest = null;
            return request;
        }
    }

    private void StartRequest(Guid propertyId)
    {
        var request = RequestAsync(propertyId);

        // Holt die Seite den Request nicht ab (z.B. abgebrochene Navigation), darf seine
        // Exception niemanden erreichen - beobachtet wird sie hier.
        _ = request.ContinueWith(
            task => logger.LogDebug(task.Exception, "[PropertyDetail] Prefetch fehlgeschlagen"),
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted,
            TaskScheduler.Default);

        lock (_gate)
        {
            _pendingId = propertyId;
            _pendingRequest = request;
        }
    }

    private async Task<GetPropertyByIdResponse?> RequestAsync(Guid propertyId)
    {
        var (_, response) = await mediator
            .Request(new GetPropertyByIdHttpRequest { Id = propertyId })
            .ConfigureAwait(false);

        trace.Mark(propertyId, "Detaildaten da");

        // Erstes Foto in der Detail-Aufloesung schon waehrend der Navigation holen -
        // der Bild-Cache buendelt gleiche URLs, die Detailseite laedt es also nicht erneut
        var firstPreview = response?.Property?.PreviewImageUrls?.FirstOrDefault(url => !string.IsNullOrEmpty(url));
        if (firstPreview != null)
            _ = imageCache.GetOrDownloadAsync(firstPreview);

        return response;
    }
}
