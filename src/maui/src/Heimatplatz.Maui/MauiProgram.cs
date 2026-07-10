using Heimatplatz.Maui.ApiClient.Configuration;
using Heimatplatz.Maui.Core.DeepLink.Configuration;
using Heimatplatz.Maui.Features.AppUpdate.Configuration;
using Heimatplatz.Maui.Features.Auth.Infrastructure;
using Heimatplatz.Maui.Features.Notifications.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.DevFlow.Agent;
using Shiny;
using Shiny.Mediator;

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
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });

        // API-Endpunkt fuer den generierten Shiny.Mediator OpenAPI-Client
        var apiBaseUrl = "https://api.heimatplatz.at";
#if DEBUG && ANDROID
        // Debug im Android-Emulator: lokale API am Host (10.0.2.2 = Host-Loopback)
        if (DeviceInfo.Current.DeviceType == DeviceType.Virtual)
        {
            apiBaseUrl = "http://10.0.2.2:5292";
        }
        else
        {
            // Physisches Geraet via USB-Debugging: "adb reverse tcp:5292 tcp:5292" leitet
            // localhost:5292 am Geraet auf die lokale API am Entwicklungsrechner um.
            apiBaseUrl = "http://localhost:5292";
        }
#endif
        // Override per Umgebungsvariable (z.B. Screenshot-Runs im Simulator gegen die
        // Test-API via SIMCTL_CHILD_HEIMATPLATZ_API_URL) - auf Geraeten nie gesetzt
        var envApiUrl = Environment.GetEnvironmentVariable("HEIMATPLATZ_API_URL");
        if (!string.IsNullOrWhiteSpace(envApiUrl))
        {
            apiBaseUrl = envApiUrl;
        }

        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mediator:Http:Heimatplatz.Maui.ApiClient.Generated.*"] = apiBaseUrl
        });

        // Shiny-Serializer VOR AddShinyMediator konfigurieren (wie Uno Core.Startup):
        // Der explizite DefaultJsonTypeInfoResolver erlaubt Deserialisierung der
        // generierten OpenAPI-Typen ohne eigenen JsonSerializerContext.
        builder.Services.ConfigureJsonSerializer(options =>
        {
            options.PropertyNameCaseInsensitive = true;
            options.TypeInfoResolverChain.Add(new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver());
        });
        builder.Services.AddJsonSerialization();

        // Shiny Mediator mit MAUI-Integration (MainThread, Event-Safety)
        builder.Services.AddShinyMediator(cfg =>
        {
            cfg.UseMaui();
            cfg.PreventEventExceptions();
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
