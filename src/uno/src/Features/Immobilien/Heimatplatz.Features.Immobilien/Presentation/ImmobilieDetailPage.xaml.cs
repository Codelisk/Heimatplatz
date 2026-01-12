using UnoFramework.Contracts.Navigation;

namespace Heimatplatz.Features.Immobilien.Presentation;

public sealed partial class ImmobilieDetailPage : Page
{
    private bool _hasLoaded;

    public ImmobilieDetailPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        this.DataContextChanged += OnDataContextChanged;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("=== ImmobilieDetailPage.OnLoaded ===");
        _hasLoaded = true;
        TryNavigationAware(null);
    }

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        Console.WriteLine($"=== ImmobilieDetailPage.DataContextChanged: {args.NewValue?.GetType().Name} ===");
        TryNavigationAware(null);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        Console.WriteLine($"=== ImmobilieDetailPage.OnNavigatedTo: {e.Parameter} (type: {e.Parameter?.GetType().Name}) ===");
        TryNavigationAware(e.Parameter);
    }

    private void TryNavigationAware(object? parameter)
    {
        if (!_hasLoaded)
        {
            Console.WriteLine("=== Detail page not loaded yet, skipping ===");
            return;
        }

        if (DataContext is INavigationAware navAware)
        {
            Console.WriteLine($"=== Calling OnNavigatedTo on ViewModel with parameter: {parameter} ===");
            navAware.OnNavigatedTo(parameter);
        }
        else
        {
            Console.WriteLine($"=== DataContext is not INavigationAware: {DataContext?.GetType().Name} ===");
        }
    }
}
