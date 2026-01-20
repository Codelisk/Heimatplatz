using Heimatplatz.Core.Startup;
using Heimatplatz.Features.Auth.Presentation;
using Heimatplatz.Features.Notifications.Presentation;
using Heimatplatz.Features.Notifications.Services;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Presentation;
using Uno.Resizetizer;
#if DEBUG
using Heimatplatz.Features.Debug.Presentation;
#endif

namespace Heimatplatz.App;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    public static Window? MainWindow { get; private set; }

    public IHost? Host { get; private set; }

    protected async override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var builder = this.CreateBuilder(args)
            .UseToolkitNavigation()
            .Configure(host => host
#if DEBUG
                .UseEnvironment(Environments.Development)
#endif
                .UseLogging(configure: (context, logBuilder) =>
                {
                    logBuilder
                        .SetMinimumLevel(
                            context.HostingEnvironment.IsDevelopment() ?
                                LogLevel.Information :
                                LogLevel.Warning)
                        .CoreLogLevel(LogLevel.Warning);
                }, enableUnoLogging: true)
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
                        .Section<AppConfig>()
                )
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
                    services.AddAppServices();
                    services.AddTransient<Shell>();
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();

#if __ANDROID__ || __IOS__ || __MACCATALYST__
        // Initialize push notifications after app startup (mobile platforms only)
        _ = InitializePushNotificationsAsync();
#endif
    }

#if __ANDROID__ || __IOS__ || __MACCATALYST__
    private async Task InitializePushNotificationsAsync()
    {
        try
        {
            // Wait a bit to let the app finish startup
            await Task.Delay(1000);

            var pushInitializer = Host?.Services.GetService<IPushNotificationInitializer>();
            if (pushInitializer != null)
            {
                await pushInitializer.InitializeAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error but don't crash the app
            System.Diagnostics.Debug.WriteLine($"Failed to initialize push notifications: {ex}");
        }
    }
#endif

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<HomePage, HomeViewModel>(),
            new ViewMap<RegisterPage, RegisterViewModel>(),
            new ViewMap<LoginPage, LoginViewModel>(),
            new ViewMap<AddPropertyPage, AddPropertyViewModel>(),
            new DataViewMap<EditPropertyPage, EditPropertyViewModel, EditPropertyData>(),
            new ViewMap<MyPropertiesPage, MyPropertiesViewModel>(),
            new ViewMap<FavoritesPage, FavoritesViewModel>(),
            new ViewMap<BlockedPage, BlockedViewModel>(),
            new ViewMap<NotificationSettingsPage, NotificationSettingsViewModel>(),
            new ViewMap<FilterPreferencesPage, FilterPreferencesViewModel>()
#if DEBUG
            , new ViewMap<DebugStartPage, DebugStartViewModel>()
#endif
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
#if DEBUG
                    new ("DebugStart", View: views.FindByViewModel<DebugStartViewModel>(), IsDefault: true),
                    new ("Home", View: views.FindByViewModel<HomeViewModel>()),
#else
                    new ("Home", View: views.FindByViewModel<HomeViewModel>(), IsDefault: true),
#endif
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Register", View: views.FindByViewModel<RegisterViewModel>()),
                    new ("Login", View: views.FindByViewModel<LoginViewModel>()),
                    new ("AddProperty", View: views.FindByViewModel<AddPropertyViewModel>()),
                    new ("EditProperty", View: views.FindByViewModel<EditPropertyViewModel>()),
                    new ("MyProperties", View: views.FindByViewModel<MyPropertiesViewModel>()),
                    new ("Favorites", View: views.FindByViewModel<FavoritesViewModel>()),
                    new ("Blocked", View: views.FindByViewModel<BlockedViewModel>()),
                    new ("NotificationSettings", View: views.FindByViewModel<NotificationSettingsViewModel>()),
                    new ("FilterPreferences", View: views.FindByViewModel<FilterPreferencesViewModel>())
                ]
            )
        );
    }
}
