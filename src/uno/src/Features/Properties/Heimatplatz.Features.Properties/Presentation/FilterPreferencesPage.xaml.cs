using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Seite fuer die Konfiguration von Benutzer-Filtereinstellungen
/// </summary>
public sealed partial class FilterPreferencesPage : Page
{
    public FilterPreferencesViewModel? ViewModel { get; set; }

    public FilterPreferencesPage()
    {
        this.InitializeComponent();
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            await ViewModel.LoadPreferencesCommand.ExecuteAsync(null);
        }
    }
}
