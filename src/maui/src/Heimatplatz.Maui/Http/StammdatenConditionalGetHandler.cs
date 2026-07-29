using System.Net;
using System.Text;
using Heimatplatz.Maui.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Http;

/// <summary>
/// Conditional GET fuer Stammdaten-Endpoints (Client-Gegenstueck zur API-seitigen
/// StammdatenConditionalGetMiddleware): sendet If-None-Match mit dem zuletzt
/// gespeicherten ETag und ersetzt ein koerperloses 304 durch eine synthetische 200
/// mit dem lokal gespeicherten Body. Fuer den generierten Mediator-HTTP-Handler und
/// den Cache-/Offline-Stack ist das transparent - ueber die Leitung gehen bei
/// unveraenderten Stammdaten nur noch Header. Liefert der Server dagegen einen
/// neuen ETag (Inhalt geaendert), wird ein <see cref="StammdatenChangedEvent"/>
/// publiziert, damit In-Memory-Kopien (z.B. LocationService) verworfen werden.
/// </summary>
public sealed class StammdatenConditionalGetHandler(
    ConditionalGetStore store,
    IServiceProvider serviceProvider,
    ILogger<StammdatenConditionalGetHandler> logger) : DelegatingHandler
{
    // Muss zur Server-Allowlist (StammdatenConditionalGetMiddleware) passen und darf
    // nur anonyme GET-Routen enthalten, deren Antwort nicht vom Benutzer abhaengt
    private static readonly string[] StammdatenPathPrefixes =
    [
        "/api/locations",
        "/api/legal/imprint",
        "/api/legal/privacy-policy"
    ];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var uri = request.RequestUri;
        if (request.Method != HttpMethod.Get || uri is null || !IsStammdatenPath(uri.AbsolutePath))
            return await base.SendAsync(request, cancellationToken);

        var stored = await store.GetAsync(uri, cancellationToken);
        if (stored is not null)
            request.Headers.TryAddWithoutValidation("If-None-Match", stored.ETag);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotModified)
        {
            // 304 kommt nur als Antwort auf unser If-None-Match, stored ist hier also
            // vorhanden - defensiv trotzdem pruefen (fehlerhafte Proxies)
            if (stored is null)
            {
                logger.LogWarning("304 ohne lokalen Conditional-GET-Eintrag fuer {Path}", uri.AbsolutePath);
                return response;
            }

            logger.LogDebug("Stammdaten unveraendert (304), lokaler Body fuer {Path}", uri.AbsolutePath);
            response.Dispose();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                RequestMessage = request,
                Content = new StringContent(stored.Body, Encoding.UTF8, "application/json")
            };
        }

        if (response.IsSuccessStatusCode && response.Headers.ETag is { IsWeak: false } etag)
        {
            // ReadAsStringAsync puffert den Content - der Mediator-Handler weiter oben
            // liest denselben gepufferten Inhalt danach problemlos erneut
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            await store.SetAsync(uri, new ConditionalGetStore.Entry(etag.Tag, body), cancellationToken);

            if (stored is not null && stored.ETag != etag.Tag)
            {
                logger.LogInformation("Stammdaten geaendert fuer {Path}", uri.AbsolutePath);
                await PublishChangedAsync(uri.AbsolutePath, cancellationToken);
            }
        }

        return response;
    }

    private static bool IsStammdatenPath(string path) =>
        StammdatenPathPrefixes.Any(prefix => path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));

    private async Task PublishChangedAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            // Lazy aufgeloest statt injiziert: haelt den Konstruktions-Graph des
            // HttpMessageHandlers frei von der Mediator-Pipeline
            var mediator = serviceProvider.GetRequiredService<IMediator>();
            await mediator.Publish(new StammdatenChangedEvent(path), cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fail-open: die Benachrichtigung darf den eigentlichen Request nie brechen
            logger.LogWarning(ex, "StammdatenChangedEvent fuer {Path} konnte nicht publiziert werden", path);
        }
    }
}
