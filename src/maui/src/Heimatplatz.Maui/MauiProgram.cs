using Heimatplatz.Maui.ApiClient.Configuration;
using Heimatplatz.Maui.Core.DeepLink.Configuration;
using Heimatplatz.Maui.Features.AppUpdate.Configuration;
using Heimatplatz.Maui.Features.Auth.Infrastructure;
using Heimatplatz.Maui.Features.Notifications.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
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
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Mediator:Http:Heimatplatz.Maui.ApiClient.Generated.*"] = "https://heimatplatz-api.azurewebsites.net"
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

        // Features
        builder.Services.AddNotificationsFeature();
        builder.Services.AddAppUpdateFeature();
        builder.Services.AddDeepLinkFeature();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
