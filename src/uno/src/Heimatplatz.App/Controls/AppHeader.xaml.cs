using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions;
using Uno.Extensions.Navigation;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Gemeinsamer App-Header fuer alle Seiten
/// Enthaelt Logo und Auth-Bereich (Login/Register oder Profil-Menue)
/// </summary>
public sealed partial class AppHeader : UserControl
{
    private ILogger<AppHeader>? _logger;

    public AppHeaderViewModel? ViewModel => DataContext as AppHeaderViewModel;

    /// <summary>
    /// NavigationView Referenz fuer Hamburger-Button
    /// </summary>
    public static readonly DependencyProperty NavigationViewProperty =
        DependencyProperty.Register(
            nameof(NavigationView),
            typeof(NavigationView),
            typeof(AppHeader),
            new PropertyMetadata(null));

    public NavigationView? NavigationView
    {
        get => GetValue(NavigationViewProperty) as NavigationView;
        set => SetValue(NavigationViewProperty, value);
    }

    public AppHeader()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Set DataContext manually after a delay to ensure Host is initialized
        if (this.DataContext == null)
        {
            this.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
            {
                if (this.DataContext == null)
                {
                    var serviceProvider = (Application.Current as App)?.Host?.Services;
                    if (serviceProvider != null)
                    {
                        this.DataContext = serviceProvider.GetService<AppHeaderViewModel>();
                        _logger = serviceProvider.GetService<ILogger<AppHeader>>();
                        _logger?.LogInformation("[AppHeader] DataContext manually set to AppHeaderViewModel");
                    }
                }
            });
        }
    }

    private void OnHamburgerButtonClick(object sender, RoutedEventArgs e)
    {
        if (NavigationView != null)
        {
            NavigationView.IsPaneOpen = !NavigationView.IsPaneOpen;
            _logger?.LogInformation("[AppHeader] Hamburger clicked - IsPaneOpen: {IsPaneOpen}", NavigationView.IsPaneOpen);
        }
        else
        {
            _logger?.LogWarning("[AppHeader] Hamburger clicked but NavigationView is null");
        }
    }
}
