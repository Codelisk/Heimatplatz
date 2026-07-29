using System.Security.Cryptography;

namespace Heimatplatz.Api;

/// <summary>
/// Conditional GET (RFC 9110) fuer Stammdaten-Endpoints: Antworten bekommen einen
/// starken Content-Hash-ETag, Requests mit passendem If-None-Match ein koerperloses
/// 304. Clients (MAUI-Offline-Cache, Browser) halten Stammdaten dadurch lokal und
/// uebertragen nur noch dann eine volle Antwort, wenn sich der Inhalt geaendert hat.
/// Die Antwort wird dafuer im Speicher gepuffert - deshalb ausschliesslich fuer die
/// kleinen, anonymen Stammdaten-Routen aktiv, nie fuer Listen oder Uploads.
/// </summary>
public sealed class StammdatenConditionalGetMiddleware(RequestDelegate next)
{
    // Explizite Liste statt Wildcard, damit niemals user-spezifische oder
    // authentifizierte Antworten einen gemeinsamen Validator bekommen.
    // "/api/admin/legal/*" bleibt unberuehrt (kein Segment-Prefix dieser Eintraege).
    private static readonly PathString[] StammdatenPaths =
    [
        new("/api/locations"),
        new("/api/legal/imprint"),
        new("/api/legal/privacy-policy"),
        new("/api/legal/contact")
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        if (!IsStammdatenRequest(context.Request))
        {
            await next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await next(context);

            if (context.Response.StatusCode == StatusCodes.Status200OK && buffer.Length > 0)
            {
                var hash = SHA256.HashData(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
                var etag = $"\"{Convert.ToHexStringLower(hash)}\"";

                // no-cache = speichern erlaubt, aber vor jeder Verwendung revalidieren
                context.Response.Headers.CacheControl = "no-cache";
                context.Response.Headers.ETag = etag;

                if (RequestMatchesETag(context.Request, etag))
                {
                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    context.Response.ContentLength = null;
                    context.Response.ContentType = null;
                    return;
                }
            }

            context.Response.Body = originalBody;
            buffer.Position = 0;
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        finally
        {
            context.Response.Body = originalBody;
        }
    }

    private static bool IsStammdatenRequest(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
            return false;

        foreach (var path in StammdatenPaths)
        {
            if (request.Path.StartsWithSegments(path))
                return true;
        }

        return false;
    }

    private static bool RequestMatchesETag(HttpRequest request, string etag)
    {
        foreach (var headerValue in request.Headers.IfNoneMatch)
        {
            if (headerValue is null)
                continue;

            if (headerValue.Trim() == "*")
                return true;

            foreach (var candidate in headerValue.Split(','))
            {
                var normalized = candidate.Trim();
                // Schwache Validatoren ("W/") gleichwertig behandeln - Inhalt ist byte-identisch
                if (normalized.StartsWith("W/", StringComparison.Ordinal))
                    normalized = normalized[2..];

                if (normalized == etag)
                    return true;
            }
        }

        return false;
    }
}
