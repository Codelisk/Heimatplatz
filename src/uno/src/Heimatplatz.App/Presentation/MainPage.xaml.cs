using Heimatplatz.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.App.Presentation;

public sealed partial class MainPage : Page
{
    private PageTitleService? _pageTitleService;
    private ILogger<MainPage>? _logger;

    public MainPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        var serviceProvider = (Application.Current as App)?.Host?.Services;
        if (serviceProvider != null)
        {
            _pageTitleService = serviceProvider.GetService<PageTitleService>();
            _logger = serviceProvider.GetService<ILogger<MainPage>>();

            // Set initial title based on current selection
            UpdatePageTitle(NavView.SelectedItem as NavigationViewItem);
        }
    }

    private void OnNavigationViewSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem selectedItem)
        {
            UpdatePageTitle(selectedItem);
        }
    }

    private void UpdatePageTitle(NavigationViewItem? selectedItem)
    {
        if (_pageTitleService == null || selectedItem == null)
            return;

        var title = selectedItem.Content?.ToString();

        // Special case: "Home" zeigt "HEIMATPLATZ"
        if (string.Equals(title, "Home", StringComparison.OrdinalIgnoreCase))
        {
            _pageTitleService.Reset();
        }
        else
        {
            _pageTitleService.SetTitle(title);
        }

        _logger?.LogInformation("[MainPage] Page title set to: {Title}", _pageTitleService.CurrentTitle);
    }
}
