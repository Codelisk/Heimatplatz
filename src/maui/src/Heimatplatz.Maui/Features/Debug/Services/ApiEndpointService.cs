using Heimatplatz.Maui.Core.Build;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Sync;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Debug.Services;

/// <summary>
/// Schreibt die Base-URL des generierten OpenAPI-Clients direkt in die IConfiguration
/// (Shiny.Mediator liest sie pro Request, daher wirkt der Wechsel sofort) und
/// persistiert die Auswahl in den Preferences fuer den naechsten App-Start.
///
/// Die lokal gespeicherten Daten muessen beim Wechsel nicht geloescht werden: sowohl
/// die Cache-/Offline-Schluessel (UserScopedContractKeyProvider) als auch der
/// Delta-Sync-Wasserstand (PropertySyncService) enthalten den Endpunkt - jede Umgebung
/// hat also ihren eigenen Datenbestand, der beim Zurueckschalten wieder auftaucht.
/// </summary>
[Singleton]
public class ApiEndpointService(
    IConfiguration configuration,
    IAuthService authService,
    IMediator mediator,
    ILogger<ApiEndpointService> logger
) : IApiEndpointService
{
    public string DevelopmentUrl => ApiEndpoints.GetDevelopmentUrl();

    public string TestUrl => ApiEndpoints.TestUrl;

    public string ProductionUrl => ApiEndpoints.ProductionUrl;

    public ApiEndpointKind SelectedEndpoint => ApiEndpoints.GetSelectedEndpoint();

    public string CurrentUrl => configuration[ApiEndpoints.MediatorHttpConfigKey] ?? ApiEndpoints.ProductionUrl;

    public event EventHandler? EndpointChanged;

    public void SetEndpoint(ApiEndpointKind kind)
    {
        // Fail-closed: In der Store-Version gibt es weder Umschalter noch Aufrufer -
        // ein Aufruf waere ein Programmierfehler und darf keine Wirkung haben.
        if (!AppChannels.AreDeveloperToolsEnabled)
        {
            logger.LogWarning("Endpunktwechsel in der Store-Version ignoriert");
            return;
        }

        Preferences.Default.Set(ApiEndpoints.EndpointPreferenceKey, kind.ToString());
        configuration[ApiEndpoints.MediatorHttpConfigKey] = ApiEndpoints.GetUrl(kind);

        EndpointChanged?.Invoke(this, EventArgs.Empty);
    }

    public async Task<bool> SwitchEndpointAsync(ApiEndpointKind kind)
    {
        if (!AppChannels.AreDeveloperToolsEnabled)
        {
            logger.LogWarning("Endpunktwechsel in der Store-Version ignoriert");
            return false;
        }

        SetEndpoint(kind);

        // Access-/Refresh-Token stammen aus der vorherigen Umgebung und werden von der
        // neuen API abgelehnt. Ohne dieses Aufraeumen laeuft die App in eine 401-Schleife,
        // die wie ein Serverfehler aussieht.
        var wasAuthenticated = authService.IsAuthenticated;
        if (wasAuthenticated)
            authService.ClearAuthentication();

        // Bereits sichtbare Listen zeigen noch die Daten der alten Umgebung. Denselben
        // Weg wie der Delta-Sync nach einem Voll-Refresh nehmen: alle offenen Sammlungen
        // laden neu - jetzt gegen die neue API.
        await mediator.Publish(new PropertyDataSyncedEvent([], [], [], FullResync: true));

        logger.LogInformation(
            "API-Endpunkt gewechselt: {Endpoint} ({Url}), Abmeldung: {LoggedOut}",
            kind,
            ApiEndpoints.GetUrl(kind),
            wasAuthenticated);

        return wasAuthenticated;
    }
}
