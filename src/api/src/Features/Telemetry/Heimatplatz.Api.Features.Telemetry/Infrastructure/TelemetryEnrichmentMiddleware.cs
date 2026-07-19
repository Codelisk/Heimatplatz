using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

namespace Heimatplatz.Api.Features.Telemetry.Infrastructure;

/// <summary>
/// Setzt user.id (sub-Claim) und client.app (X-Client-App-Header) als Tags auf die
/// Request-Activity. Muss nach UseAuthentication laufen; bewusst Middleware statt
/// EnrichWithHttpResponse - das feuert erst beim Activity-Stop und damit zu spaet
/// fuer Logs, die waehrend des Requests entstehen.
/// </summary>
public class TelemetryEnrichmentMiddleware(RequestDelegate next)
{
    public const string ClientAppHeader = "X-Client-App";

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            var userId = context.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            if (!string.IsNullOrEmpty(userId))
                activity.SetTag("user.id", userId);

            if (context.Request.Headers.TryGetValue(ClientAppHeader, out var clientApp)
                && !string.IsNullOrEmpty(clientApp.ToString()))
            {
                activity.SetTag("client.app", clientApp.ToString());
            }
        }

        await next(context);
    }
}

public static class TelemetryEnrichmentMiddlewareExtensions
{
    public static IApplicationBuilder UseTelemetryEnrichment(this IApplicationBuilder app)
        => app.UseMiddleware<TelemetryEnrichmentMiddleware>();
}
