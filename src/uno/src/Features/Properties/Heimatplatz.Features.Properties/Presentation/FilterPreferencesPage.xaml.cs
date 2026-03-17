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

    private void OnOrtSelectionChanged(object? sender, EventArgs e)
    {
        // Push the selected orte from OrtPicker directly to ViewModel
        // (TwoWay binding with List<string> is unreliable)
        if (ViewModel != null)
        {
            ViewModel.SelectedOrte = OrtPickerControl.GetSelectedOrte();
        }
    }
}
