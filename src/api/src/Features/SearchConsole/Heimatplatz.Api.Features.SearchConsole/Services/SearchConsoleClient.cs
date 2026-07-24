using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.SearchConsole.v1;
using Google.Apis.SearchConsole.v1.Data;
using Heimatplatz.Api;
using Heimatplatz.Api.Features.SearchConsole.Configuration;
using Heimatplatz.Api.Features.SearchConsole.Contracts.Mediator.Requests;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.SearchConsole.Services;

/// <summary>
/// Server-zu-Server-Client fuer die Google Search Console API (searchanalytics.query).
/// Auth ueber einen Service-Account-JSON-Key (kein OAuth-Consent-Flow - der Service-Account
/// wird als "Eingeschraenkter" Nutzer in Search Console zur Property hinzugefuegt, siehe
/// README). Fail-soft wie die Firebase/APNs-Konfiguration im Notifications-Feature:
/// Ein fehlender/ungueltiger Key oder ein API-Fehler
/// werfen keine Exception, sondern liefern Enabled=false - die Intern-Seite zeigt dann nur
/// einen Hinweis statt eines 500ers.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class SearchConsoleClient(IOptions<SearchConsoleOptions> options, ILogger<SearchConsoleClient> logger) : ISearchConsoleClient
{
    private const int TopRowLimit = 10;
    private const int WindowDays = 28;

    // Search-Console-Daten sind erst nach ein paar Tagen final - die aktuellsten Tage auslassen.
    private const int DelayDays = 3;

    public async Task<GetSearchConsoleSummaryResponse> GetSummaryAsync(CancellationToken cancellationToken)
    {
        var config = options.Value;
        var credentialPath = ResolveCredentialPath(config.ServiceAccountPath);
        if (credentialPath is null)
        {
            return new GetSearchConsoleSummaryResponse { Enabled = false };
        }

        try
        {
            using var service = CreateService(credentialPath);

            var endDate = DateTime.UtcNow.Date.AddDays(-DelayDays);
            var startDate = endDate.AddDays(-WindowDays);

            var queryRows = await QueryAsync(service, config.SiteUrl, startDate, endDate, "query", cancellationToken);
            var pageRows = await QueryAsync(service, config.SiteUrl, startDate, endDate, "page", cancellationToken);

            var clicksTotal = queryRows.Sum(r => r.Clicks ?? 0);
            var impressionsTotal = queryRows.Sum(r => r.Impressions ?? 0);

            return new GetSearchConsoleSummaryResponse
            {
                Enabled = true,
                ClicksTotal = (int)clicksTotal,
                ImpressionsTotal = (int)impressionsTotal,
                AverageCtr = impressionsTotal > 0 ? clicksTotal / impressionsTotal : 0,
                AveragePosition = queryRows.Count > 0 ? queryRows.Average(r => r.Position ?? 0) : 0,
                TopQueries = queryRows.Select(ToRowDto).ToList(),
                TopPages = pageRows.Select(ToRowDto).ToList(),
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Search-Console-Abfrage fuer {SiteUrl} fehlgeschlagen", config.SiteUrl);
            return new GetSearchConsoleSummaryResponse { Enabled = false };
        }
    }

    private static SearchConsoleService CreateService(string credentialPath)
    {
        // GoogleCredential.FromStream/FromFile sind seit Google.Apis.Auth als
        // Sicherheitsrisiko deprecatet - CredentialFactory ist der empfohlene Ersatz.
        var credential = CredentialFactory.FromFile<ServiceAccountCredential>(credentialPath)
            .ToGoogleCredential()
            .CreateScoped(SearchConsoleService.Scope.WebmastersReadonly);

        return new SearchConsoleService(new BaseClientService.Initializer
        {
            HttpClientInitializer = credential,
            ApplicationName = "Heimatplatz",
        });
    }

    private static async Task<List<ApiDataRow>> QueryAsync(
        SearchConsoleService service, string siteUrl, DateTime startDate, DateTime endDate, string dimension, CancellationToken cancellationToken)
    {
        var request = new SearchAnalyticsQueryRequest
        {
            StartDate = startDate.ToString("yyyy-MM-dd"),
            EndDate = endDate.ToString("yyyy-MM-dd"),
            Dimensions = [dimension],
            RowLimit = TopRowLimit,
        };

        var response = await service.Searchanalytics.Query(request, siteUrl).ExecuteAsync(cancellationToken);
        return response.Rows?.ToList() ?? [];
    }

    private static SearchConsoleRowDto ToRowDto(ApiDataRow row) => new()
    {
        Label = row.Keys?.FirstOrDefault() ?? "",
        Clicks = (int)(row.Clicks ?? 0),
        Impressions = (int)(row.Impressions ?? 0),
        Ctr = row.Ctr ?? 0,
        Position = row.Position ?? 0,
    };

    /// <summary>Gleiche Aufloese-Logik wie PushProvidersConfiguration.ResolveCredentialPath.</summary>
    private static string? ResolveCredentialPath(string? configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            return null;

        if (Path.IsPathRooted(configuredPath))
            return File.Exists(configuredPath) ? configuredPath : null;

        var outputPath = Path.Combine(AppContext.BaseDirectory, configuredPath);
        if (File.Exists(outputPath))
            return outputPath;

        var workingDirectoryPath = Path.GetFullPath(configuredPath);
        return File.Exists(workingDirectoryPath) ? workingDirectoryPath : null;
    }
}
