using Heimatplatz.Maui.ApiClient.Configuration;
using Heimatplatz.Maui.Core.DeepLink.Configuration;
using Heimatplatz.Maui.Features.AppUpdate.Configuration;
using Heimatplatz.Maui.Features.Auth.Infrastructure;
using Heimatplatz.Maui.Features.Debug.Services;
using Heimatplatz.Maui.Features.Notifications.Configuration;
using Heimatplatz.Maui.Offline;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.DevFlow.Agent;
using Shiny;
using Shiny.DocumentDb;
using Shiny.DocumentDb.Sqlite;
using Shiny.Mediator;
using Shiny.Mediator.Infrastructure;

namespace Heimatplatz.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        // Muss vor dem ersten Store-Zugriff laufen (Screenshot-Runs im Simulator)
        Core.Screenshots.ScreenshotMode.TryOverrideSecureStore();

        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseShiny()
            .UseShinyShell(x => x.AddGeneratedMaps())
            .UseShinyControls()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // API-Endpunkt fuer den generierten Shiny.Mediator OpenAPI-Client
        var apiBaseUrl = ApiEndpoints.ProductionUrl;
#if DEBUG
        // Debug: zuletzt auf der DebugPage (Flyout "Debug") gewaehlter Endpunkt
        // (Entwicklung/Test/Produktion); Default je Plattform: Android lokal,
        // sonst Produktion. Zur Laufzeit umschaltbar.
        apiBaseUrl = ApiEndpoints.GetUrl(ApiEndpoints.GetSelectedEndpoint());
#endif
        // Override per Umgebungsvariable (z.B. Screenshot-Runs im Simulator gegen die
        // Test-API via SIMCTL_CHILD_HEIMATPLATZ_API_URL) - auf Geraeten nie gesetzt
        var envApiUrl = Environment.GetEnvironmentVariable("HEIMATPLATZ_API_URL");
        if (!string.IsNullOrWhiteSpace(envApiUrl))
        {
            apiBaseUrl = envApiUrl;
        }

        var mediatorConfiguration = new Dictionary<string, string?>
        {
            [ApiEndpoints.MediatorHttpConfigKey] = apiBaseUrl
        };
        OfflineDataConfiguration.AddTo(mediatorConfiguration);
        builder.Configuration.AddInMemoryCollection(mediatorConfiguration);

        // Shiny-Serializer VOR AddShinyMediator konfigurieren (wie Uno Core.Startup):
        // Der explizite DefaultJsonTypeInfoResolver erlaubt Deserialisierung der
        // generierten OpenAPI-Typen ohne eigenen JsonSerializerContext.
        builder.Services.ConfigureJsonSerializer(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
        });
        builder.Services.AddJsonSerialization();

        // Echte lokale SQLite-Datenbank fuer Offline-Antworten und Mediator-Cache.
        // AppDataDirectory (nicht CacheDirectory) verhindert, dass das Betriebssystem
        // die Offline-Daten bei Speicherknappheit entfernt.
        var offlineDatabasePath = Path.Combine(FileSystem.Current.AppDataDirectory, "heimatplatz-offline.db");
        builder.Services.AddDocumentStore(options =>
        {
            options.DatabaseProvider = new SqliteDatabaseProvider($"Data Source={offlineDatabasePath}");
            options.JsonSerializerOptions = OfflineStorageJsonContext.Default.Options;
            options.UseReflectionFallback = false;
            options.MapTypeToTable<MediatorStorageRecord>("mediator_storage");
        });
        builder.Services.AddSingleton<IStorageService, ShinyDocumentStorageService>();
        builder.Services.AddSingleton<IContractKeyProvider, UserScopedContractKeyProvider>();
        builder.Services.AddSingleton<LocalFirstRefreshCoordinator>();

        // Reihenfolge im Request-Pipeline:
        // lokal sofort -> persistenter Cache -> Offline-Fallback -> Netzwerk-Schranke
        // -> Token-Refresh -> generierter HTTP-Handler.
        builder.Services.AddShinyMediator(cfg =>
        {
            cfg.AddOpenRequestMiddleware(
                typeof(LocalFirstRequestMiddleware<,>),
                ServiceLifetime.Singleton);
            cfg.AddMauiPersistentCache();
            cfg.UseMaui();
            cfg.PreventEventExceptions();
            cfg.AddOpenRequestMiddleware(
                typeof(OfflineNetworkGuardMiddleware<,>),
                ServiceLifetime.Singleton);
            // 401-Handling: Token-Refresh + Auth-State-Cleanup
            cfg.AddOpenRequestMiddleware(typeof(TokenRefreshMiddleware<,>), ServiceLifetime.Singleton);
            cfg.Services.AddSingleton<IExceptionHandler, AuthExceptionHandler>();
        });

        // Shiny Stores (Key/Value-Persistenz, [Bind]-Properties)
        builder.Services.AddShinyStores();

        // Alle [Singleton]/[Service]-Klassen dieser Assembly (Shiny DI Source Generator)
        builder.Services.AddGeneratedServices();

        // AppStartupService ist AsSelf registriert (App-Konstruktor braucht den konkreten Typ);
        // das Logout-Event muss denselben Singleton treffen
        builder.Services.AddSingleton<IEventHandler<Events.LogoutRequestedEvent>>(
            sp => sp.GetRequiredService<Services.AppStartupService>());

        // Generierte OpenAPI-HTTP-Handler (Shiny.Mediator MediatorHttp, Projekt Heimatplatz.Maui.ApiClient)
        builder.Services.AddApiClientFeature();

#if ANDROID || IOS
        // Shiny.Speech: Speech-to-Text fuer das Diktat der KI-gestuetzten Inseratserstellung (Phones)
        builder.Services.AddSpeechToText();
#endif

        // Features
        builder.Services.AddNotificationsFeature();
        builder.Services.AddAppUpdateFeature();
        builder.Services.AddDeepLinkFeature();

#if DEBUG
        builder.Logging.AddDebug();
        builder.AddMauiDevFlowAgent();
#endif

        return builder.Build();
    }
}
