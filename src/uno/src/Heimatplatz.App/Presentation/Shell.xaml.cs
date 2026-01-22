using Heimatplatz.Features.Auth.Presentation;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.App.Presentation;

public sealed partial class Shell : UserControl, IContentControlProvider
{
    /// <summary>
    /// Pages auf denen der Header ausgeblendet wird (Auth-Pages)
    /// </summary>
    private static readonly HashSet<Type> HideHeaderPages =
    [
        typeof(LoginPage),
        typeof(RegisterPage)
    ];

    public ContentControl ContentControl => Splash;

    public Frame NavigationFrame => RootFrame;

    public Shell()
    {
        this.InitializeComponent();

        // Manual DataContext initialization on Loaded (Shell is not a navigable View)
        this.Loaded += OnLoaded;

        // Navigation-Events abonnieren um Header bei Auth-Pages auszublenden
        RootFrame.Navigated += OnFrameNavigated;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set DataContext manually since Shell is not a navigable View
        var serviceProvider = (Application.Current as App)?.Host?.Services;
        if (serviceProvider != null && this.DataContext == null)
        {
            this.DataContext = serviceProvider.GetRequiredService<ShellViewModel>();
        }
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        // Header ausblenden wenn auf Auth-Page
        var shouldHideHeader = e.SourcePageType != null && HideHeaderPages.Contains(e.SourcePageType);
        AppHeaderControl.Visibility = shouldHideHeader ? Visibility.Collapsed : Visibility.Visible;
    }
}
