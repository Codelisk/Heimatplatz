namespace Heimatplatz.Features.Immobilien.Presentation;

public sealed partial class ImmobilienPage : Page
{
    private bool _hasLoaded;

    public ImmobilienPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
        this.DataContextChanged += OnDataContextChanged;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("=== ImmobilienPage.OnLoaded ===");
        _hasLoaded = true;
        await TryLoadDataAsync();
    }

    private async void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        Console.WriteLine($"=== DataContextChanged: {args.NewValue?.GetType().Name} ===");
        await TryLoadDataAsync();
    }

    private async Task TryLoadDataAsync()
    {
        if (!_hasLoaded)
        {
            Console.WriteLine("=== Not loaded yet, skipping ===");
            return;
        }

        if (DataContext is ImmobilienViewModel vm)
        {
            Console.WriteLine("=== Calling LoadCommand ===");
            await vm.LoadCommand.ExecuteAsync(null);
        }
        else
        {
            Console.WriteLine($"=== DataContext is not ImmobilienViewModel: {DataContext?.GetType().Name} ===");
        }
    }
}
