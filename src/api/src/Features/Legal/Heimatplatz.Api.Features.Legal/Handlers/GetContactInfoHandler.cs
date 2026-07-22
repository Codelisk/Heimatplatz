using Heimatplatz.Api;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Services;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Handlers;

/// <summary>
/// Handler fuer GetContactInfoRequest - liefert die Kontaktdaten fuer alle Frontends.
/// Oeffentlich wie /imprint und /privacy-policy.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/legal")]
public class GetContactInfoHandler(IContactInfoProvider contactInfoProvider)
    : IRequestHandler<GetContactInfoRequest, GetContactInfoResponse>
{
    [MediatorHttpGet("/contact", OperationId = "GetContactInfo")]
    public async Task<GetContactInfoResponse> Handle(GetContactInfoRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        var contact = await contactInfoProvider.GetAsync(cancellationToken);
        return new GetContactInfoResponse(contact);
    }
}
