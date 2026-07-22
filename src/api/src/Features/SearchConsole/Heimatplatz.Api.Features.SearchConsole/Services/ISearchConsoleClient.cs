using Heimatplatz.Api.Features.SearchConsole.Contracts.Mediator.Requests;

namespace Heimatplatz.Api.Features.SearchConsole.Services;

public interface ISearchConsoleClient
{
    /// <summary>
    /// Liefert die Suchperformance der letzten 28 Tage. Fail-soft: ohne konfigurierten
    /// Service-Account-Key kommt <see cref="GetSearchConsoleSummaryResponse.Enabled"/>=false
    /// zurueck statt einer Exception.
    /// </summary>
    Task<GetSearchConsoleSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken);
}
