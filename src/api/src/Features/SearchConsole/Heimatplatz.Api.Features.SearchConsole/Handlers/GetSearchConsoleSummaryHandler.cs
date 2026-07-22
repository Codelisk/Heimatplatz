using Heimatplatz.Api;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.SearchConsole.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.SearchConsole.Services;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.SearchConsole.Handlers;

/// <summary>
/// Suchperformance-Widget im Intern-Bereich (/intern/analytics). X-Admin-Key-Schutz
/// wie alle /api/admin-Endpoints.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/search-console")]
public class GetSearchConsoleSummaryHandler(
    ISearchConsoleClient client,
    IAdminAccessGuard accessGuard
) : IRequestHandler<GetSearchConsoleSummaryRequest, GetSearchConsoleSummaryResponse>
{
    [MediatorHttpGet("/summary", OperationId = "GetSearchConsoleSummary")]
    public async Task<GetSearchConsoleSummaryResponse> Handle(GetSearchConsoleSummaryRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();
        return await client.GetSummaryAsync(cancellationToken);
    }
}
