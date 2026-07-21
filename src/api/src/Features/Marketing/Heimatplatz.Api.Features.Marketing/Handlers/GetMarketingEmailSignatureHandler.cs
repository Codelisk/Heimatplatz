using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Services;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Signatur-Vorschau fuer die Compose-Seite im Intern-Bereich: erlaubt selbst
/// geschriebene E-Mails ohne vorherige KI-Generierung (die lieferte die Signatur
/// bisher mit). X-Admin-Key-Schutz wie alle /api/admin-Endpoints.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class GetMarketingEmailSignatureHandler(
    IAdminAccessGuard accessGuard,
    IMarketingEmailComposer composer
) : IRequestHandler<GetMarketingEmailSignatureRequest, GetMarketingEmailSignatureResponse>
{
    [MediatorHttpGet("/email/signature", OperationId = "GetAdminMarketingEmailSignature")]
    public async Task<GetMarketingEmailSignatureResponse> Handle(GetMarketingEmailSignatureRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();
        var signature = await composer.GetSignatureTextAsync(cancellationToken);
        return new GetMarketingEmailSignatureResponse(signature);
    }
}
