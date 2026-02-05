using Heimatplatz.App.Controls;
using Heimatplatz.App.Presentation;
using Heimatplatz.Core.DeepLink.Services;
using Heimatplatz.Core.Startup;
using Heimatplatz.Events;
using Heimatplatz.Features.AppUpdate.Contracts.Mediator.Commands;
using Heimatplatz.Features.Auth.Presentation;
using Heimatplatz.Features.Notifications.Presentation;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Controls;
using Heimatplatz.Features.Properties.Presentation;
using Serilog;
using Shiny.Mediator;
using Uno.Resizetizer;
using UnoFramework.Contracts.Application;
using Windows.ApplicationModel.Activation;
#if __ANDROID__ || __IOS__ || __MACCATALYST__
using Shiny;
#endif
#if DEBUG
using Heimatplatz.Features.Debug.Presentation;
#endif

namespace Heimatplatz.App;

public partial class App : Application, IApplicationWithServices
{
    public App()
    {
        this.InitializeComponent();

        // Log unhandled exceptions and prevent non-fatal crashes
        this.UnhandledException += (sender, e) =>
        {
            // Uno Platform internal errors (e.g. ScrollBar VisualState race conditions)
            // are non-fatal and should not crash the app
            if (IsUnoInternalError(e.Exception))
            {
                Log.Warning(e.Exception, "Suppressed Uno internal error");
                e.Handled = true;
                return;
            }

            Log.Fatal(e.Exception, "Unhandled exception");
            Log.CloseAndFlush();
        };

        TaskScheduler.UnobservedTaskException += (sender, e) =>
        {
            Log.Error(e.Exception, "Unobserved task exception");
            e.SetObserved();
        };
    }

    public static Window? MainWindow { get; private set; }

    public IHost? Host { get; private set; }

    /// <summary>
    /// Gets the service provider for the application.
    /// Implements IApplicationWithServices for framework-level service access.
    /// </summary>
    public IServiceProvider? Services => Host?.Services;

    protected async override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
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
                .UseSerilog(
                    consoleLoggingEnabled: true,
                    fileLoggingEnabled: false, // We configure file sink manually below
                    configureLogger: config =>
                    {
                        var logPath = GetLogFilePath();
                        config
                            .WriteTo.File(
                                path: logPath,
                                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}",
                                rollingInterval: RollingInterval.Day,
                                rollOnFileSizeLimit: true,
                                fileSizeLimitBytes: 10_000_000,
                                retainedFileCountLimit: 7);
                    })
                .UseConfiguration(configure: configBuilder =>
                    configBuilder
                        .EmbeddedSource<App>()
#if __ANDROID__
                        .EmbeddedSource<App>("android")
#endif
                        .Section<AppConfig>()
                )
                .UseLocalization()
                .ConfigureServices((context, services) =>
                {
#if DEBUG
                    // Debug: Lokale Einstellung pruefen ob Production-API verwendet werden soll
                    if (Features.Debug.Presentation.DebugStartViewModel.GetUseProductionApiSetting())
                    {
                        context.Configuration[Features.Debug.Presentation.DebugStartViewModel.MediatorHttpConfigKey]
                            = Features.Debug.Presentation.DebugStartViewModel.ProductionUrl;
                    }
#endif
                    services.AddAppServices(context.Configuration);
                })
                .UseNavigation(RegisterRoutes)
            );
        MainWindow = builder.Window;
        MainWindow.Title = "Heimatplatz";

#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();

        MainWindow.Closed += (s, e) =>
        {
            Log.Information("Application shutting down");
            Log.CloseAndFlush();
        };

#if __ANDROID__
        // Initialize Shiny for Android push notifications
        if (Android.App.Application.Context is Android.App.Application app && Host?.Services != null)
        {
            // Get current activity from Uno Platform
            var currentActivity = Uno.UI.ContextHelper.Current as Android.App.Activity;
            AndroidShinyHost.Init(app, Host.Services, currentActivity);
        }

        // Check for app updates via Mediator (fire-and-forget, non-blocking)
        _ = Host.Services.GetRequiredService<IMediator>().Send(new CheckForAppUpdateCommand());
#elif __IOS__ || __MACCATALYST__
        // Initialize Shiny for iOS/Mac push notifications
        if (Host?.Services != null)
        {
            IosShinyHost.Init(Host.Services);
        }
#endif
    }

    /// <summary>
    /// Handles protocol activation (deep links) when app is launched or already running
    /// </summary>
    protected override void OnActivated(IActivatedEventArgs args)
    {
        base.OnActivated(args);

        if (args.Kind == ActivationKind.Protocol && args is ProtocolActivatedEventArgs protocolArgs)
        {
            _ = HandleProtocolActivationAsync(protocolArgs.Uri, args.PreviousExecutionState);
        }
    }

    private async Task HandleProtocolActivationAsync(Uri uri, ApplicationExecutionState previousState)
    {
        // If app wasn't running, we need to wait for Host to be initialized
        if (Host == null)
        {
            // App is starting fresh - OnLaunched hasn't completed yet
            // Wait a bit for initialization to complete
            var maxWait = 50; // 5 seconds max
            while (Host == null && maxWait > 0)
            {
                await Task.Delay(100);
                maxWait--;
            }
        }

        if (Host?.Services == null)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] Cannot handle deep link - Host not initialized: {uri}");
            return;
        }

        var deepLinkService = Host.Services.GetService<IDeepLinkService>();
        if (deepLinkService == null)
        {
            System.Diagnostics.Debug.WriteLine($"[DeepLink] DeepLinkService not available: {uri}");
            return;
        }

        System.Diagnostics.Debug.WriteLine($"[DeepLink] Handling protocol activation: {uri}");
        await deepLinkService.HandleDeepLinkAsync(uri);
    }

    private static bool IsUnoInternalError(Exception ex)
    {
        var stackTrace = ex.StackTrace;
        if (stackTrace is null) return false;

        // ResourceResolver.PopScope bug in ScrollBar VisualState transitions
        if (ex is InvalidOperationException
            && ex.Message.Contains("Base scope")
            && stackTrace.Contains("Uno.UI.ResourceResolver"))
        {
            return true;
        }

        return false;
    }

    private static string GetLogFilePath()
    {
        string baseDir;

#if __ANDROID__ || __IOS__ || __MACCATALYST__
        baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#elif HAS_UNO_SKIA
        baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Heimatplatz");
#else
        baseDir = AppDomain.CurrentDomain.BaseDirectory;
#endif

        var logDir = Path.Combine(baseDir, "Logs");
        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "heimatplatz-.log");
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<AppHeaderLeft, AppHeaderLeftViewModel>(),
            new ViewMap<AppHeaderRight, AppHeaderRightViewModel>(),
            new ViewMap<HomePage, HomeViewModel>(),
            new ViewMap<RegisterPage, RegisterViewModel>(),
            new ViewMap<LoginPage, LoginViewModel>(),
            new ViewMap<AddPropertyPage, AddPropertyViewModel>(),
            new DataViewMap<EditPropertyPage, EditPropertyViewModel, EditPropertyData>(),
            new ViewMap<MyPropertiesPage, MyPropertiesViewModel>(),
            new ViewMap<FavoritesPage, FavoritesViewModel>(),
            new ViewMap<BlockedPage, BlockedViewModel>(),
            new DataViewMap<PropertyDetailPage, PropertyDetailViewModel, PropertyDetailData>(),
            new DataViewMap<ForeclosureDetailPage, ForeclosureDetailViewModel, ForeclosureDetailData>(),
            new ViewMap<NotificationSettingsPage, NotificationSettingsViewModel>(),
            new ViewMap<FilterPreferencesPage, FilterPreferencesViewModel>(),
            new ViewMap<PrivacyPolicyPage, PrivacyPolicyViewModel>(),
            new ViewMap<HomeFilterBar, HomeFilterBarViewModel>(),
            new ViewMap<UserProfilePage, UserProfileViewModel>()
#if DEBUG
            , new ViewMap<DebugStartPage, DebugStartViewModel>()
            , new ViewMap<TestPushPage, TestPushViewModel>()
#endif
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    // Main acts as a region container for app content
                    new ("Main", View: views.FindByViewModel<MainViewModel>(),
                        IsDefault: true,
                        Nested:
                        [
                            // Header Regions - 3-column layout: Left | Main | Right
                            new ("HeaderLeft", View: views.FindByView<AppHeaderLeft>()),
                            new ("HeaderRight", View: views.FindByView<AppHeaderRight>()),
                            // HeaderMain Route - explicitly navigated when needed
                            new ("HeaderMain", View: views.FindByViewModel<HomeFilterBarViewModel>()),
                            // Content pages - NavigationView routes to Content region
                            new ("Home", View: views.FindByViewModel<HomeViewModel>(), IsDefault: true),
                            new ("MyProperties", View: views.FindByViewModel<MyPropertiesViewModel>()),
                            new ("Favorites", View: views.FindByViewModel<FavoritesViewModel>()),
                            new ("Blocked", View: views.FindByViewModel<BlockedViewModel>()),
                            new ("AddProperty", View: views.FindByViewModel<AddPropertyViewModel>()),
                            new ("EditProperty", View: views.FindByViewModel<EditPropertyViewModel>()),
                            new ("PropertyDetail", View: views.FindByViewModel<PropertyDetailViewModel>()),
                            new ("ForeclosureDetail", View: views.FindByViewModel<ForeclosureDetailViewModel>()),
                            new ("NotificationSettings", View: views.FindByViewModel<NotificationSettingsViewModel>()),
                            new ("FilterPreferences", View: views.FindByViewModel<FilterPreferencesViewModel>()),
                            new ("PrivacyPolicy", View: views.FindByViewModel<PrivacyPolicyViewModel>()),
                            new ("UserProfile", View: views.FindByViewModel<UserProfileViewModel>())
                        ]),
#if DEBUG
                    new ("DebugStart", View: views.FindByViewModel<DebugStartViewModel>()),
                    new ("TestPush", View: views.FindByViewModel<TestPushViewModel>()),
#endif
                    // Auth pages remain at Shell level (not in regions)
                    new ("Register", View: views.FindByViewModel<RegisterViewModel>()),
                    new ("Login", View: views.FindByViewModel<LoginViewModel>())
                ]
            )
        );
    }
}
