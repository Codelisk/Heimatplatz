using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Erstellt einen Marketing-E-Mail-Entwurf (Betreff + Text) aus Stichwoertern.
/// Nur fuer den Intern-Bereich: X-Admin-Key-Schutz wie alle /api/admin-Endpoints
/// (AdminAccessGuard, fail-closed; zusaetzlich Caddy-IP-Sperre auf /api/admin*).
/// Fehler der KI-Generierung werden als Success=false + Error zurueckgegeben,
/// damit der Intern-Bereich die Ursache anzeigen kann.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GenerateMarketingEmailHandler(
    IAdminAccessGuard accessGuard,
    IMarketingEmailGenerator generator,
    IMarketingEmailComposer composer,
    ILogger<GenerateMarketingEmailHandler> logger
) : IRequestHandler<GenerateMarketingEmailRequest, GenerateMarketingEmailResponse>
{
    [MediatorHttpPost("/email/generate", OperationId = "GenerateAdminMarketingEmail")]
    public async Task<GenerateMarketingEmailResponse> Handle(GenerateMarketingEmailRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        if (string.IsNullOrWhiteSpace(request.Keywords))
            return new GenerateMarketingEmailResponse(false, null, null, null, "Stichwörter dürfen nicht leer sein.");

        try
        {
            var draft = await generator.GenerateAsync(request.Keywords, request.RecipientName, cancellationToken);
            var signature = await composer.GetSignatureTextAsync(cancellationToken);
            return new GenerateMarketingEmailResponse(true, draft.Subject, draft.Body, signature, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlertext bewusst durchreichen: Aufrufer ist ausschliesslich der
            // Admin-Key-authentifizierte Astro-SSR-Server des Intern-Bereichs.
            logger.LogError(ex, "[Marketing] E-Mail-Generierung fehlgeschlagen");
            return new GenerateMarketingEmailResponse(false, null, null, null, ex.Message);
        }
    }
}
