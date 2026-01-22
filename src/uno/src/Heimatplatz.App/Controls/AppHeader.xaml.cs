using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Uno.Extensions;
using Uno.Extensions.Navigation;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Gemeinsamer App-Header fuer alle Seiten
/// Enthaelt Logo und Auth-Bereich (Login/Register oder Profil-Menue)
/// </summary>
public sealed partial class AppHeader : UserControl
{
    private INavigator? _navigator;
    private ILogger<AppHeader>? _logger;

    public AppHeaderViewModel? ViewModel => DataContext as AppHeaderViewModel;

    /// <summary>
    /// Optionaler Content fuer den mittleren Bereich (z.B. Filter)
    /// </summary>
    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(object),
            typeof(AppHeader),
            new PropertyMetadata(null));

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
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

    private async void OnMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (sender is MenuFlyoutItem menuItem && menuItem.Tag is string route)
        {
            // Lazy initialize dependencies from App's Host
            if (_navigator == null || _logger == null)
            {
                var serviceProvider = (Application.Current as App)?.Host?.Services;
                if (serviceProvider != null)
                {
                    _navigator = serviceProvider.GetService<INavigator>();
                    _logger = serviceProvider.GetService<ILogger<AppHeader>>();
                }
            }

            _logger?.LogInformation("[AppHeader] Navigation zu: {Route}", route);
            try
            {
                if (_navigator != null)
                {
                    // Use MainWindow as context instead of this UserControl
                    object context = App.MainWindow ?? (object)this;
                    // Use -/ prefix for navigation like in XAML Navigation.Request
                    await _navigator.NavigateRouteAsync(context, $"-/{route}");
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "[AppHeader] Fehler bei Navigation zu: {Route}", route);
            }
        }
    }
}
