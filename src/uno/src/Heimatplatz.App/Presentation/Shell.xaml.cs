using Heimatplatz.App.Controls;
using Heimatplatz.Features.Auth.Presentation;

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

        // Navigation-Events abonnieren um Header bei Auth-Pages auszublenden
        RootFrame.Navigated += OnFrameNavigated;
    }

    private void OnFrameNavigated(object sender, NavigationEventArgs e)
    {
        // Header ausblenden wenn auf Auth-Page
        var shouldHideHeader = e.SourcePageType != null && HideHeaderPages.Contains(e.SourcePageType);
        AppHeaderControl.Visibility = shouldHideHeader ? Visibility.Collapsed : Visibility.Visible;
    }
}
