using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Core.Startup;
using Heimatplatz.Features.Immobilien.Presentation;
using Uno.Resizetizer;

namespace Heimatplatz.App;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
    }

    protected Window? MainWindow { get; private set; }

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

        // Hot Design Studio disabled to clean up header UI
#if DEBUG
        MainWindow.UseStudio();
#endif
        MainWindow.SetWindowIcon();

        Host = await builder.NavigateAsync<Shell>();
    }

    private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
    {
        views.Register(
            new ViewMap(ViewModel: typeof(ShellViewModel)),
            new ViewMap<MainPage, MainViewModel>(),
            new ViewMap<ImmobilienPage, ImmobilienViewModel>(),
            new DataViewMap<ImmobilieDetailPage, ImmobilieDetailViewModel, ImmobilieListeDto>()
        );

        routes.Register(
            new RouteMap("", View: views.FindByViewModel<ShellViewModel>(),
                Nested:
                [
                    new ("Main", View: views.FindByViewModel<MainViewModel>()),
                    new ("Immobilien", View: views.FindByViewModel<ImmobilienViewModel>()),
                    new ("ImmobilieDetail", View: views.FindByViewModel<ImmobilieDetailViewModel>())
                ]
            )
        );
    }
}
