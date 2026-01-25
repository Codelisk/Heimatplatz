using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Seite fuer die Konfiguration von Benutzer-Filtereinstellungen
/// Inherits from BasePage for automatic header handling
/// </summary>
public sealed partial class FilterPreferencesPage : BasePage
{
    public FilterPreferencesViewModel? ViewModel => DataContext as FilterPreferencesViewModel;

    public FilterPreferencesPage()
    {
        this.InitializeComponent();
    }
}
