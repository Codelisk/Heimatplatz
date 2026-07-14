using Microsoft.Extensions.Configuration;
using Shiny;

namespace Heimatplatz.Maui.Features.Debug.Services;

/// <summary>
/// Schreibt die Base-URL des generierten OpenAPI-Clients direkt in die IConfiguration
/// (Shiny.Mediator liest sie pro Request, daher wirkt der Wechsel sofort) und
/// persistiert die Auswahl in den Preferences fuer den naechsten App-Start.
/// </summary>
[Singleton]
public class ApiEndpointService(IConfiguration configuration) : IApiEndpointService
{
    public string DevelopmentUrl => ApiEndpoints.GetDevelopmentUrl();

    public string TestUrl => ApiEndpoints.TestUrl;

    public string ProductionUrl => ApiEndpoints.ProductionUrl;

    public ApiEndpointKind SelectedEndpoint => ApiEndpoints.GetSelectedEndpoint();

    public string CurrentUrl => configuration[ApiEndpoints.MediatorHttpConfigKey] ?? ApiEndpoints.ProductionUrl;

    public void SetEndpoint(ApiEndpointKind kind)
    {
        Preferences.Default.Set(ApiEndpoints.EndpointPreferenceKey, kind.ToString());
        configuration[ApiEndpoints.MediatorHttpConfigKey] = ApiEndpoints.GetUrl(kind);
    }
}
