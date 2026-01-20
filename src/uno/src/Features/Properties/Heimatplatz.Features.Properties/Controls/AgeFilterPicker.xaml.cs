using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// Picker fuer Alters-Filter von Immobilien-Eintraegen
/// </summary>
public sealed partial class AgeFilterPicker : UserControl
{
    public AgeFilterPicker()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Ausgewaehlter Alters-Filter
    /// </summary>
    public static readonly DependencyProperty SelectedAgeFilterProperty =
        DependencyProperty.Register(
            nameof(SelectedAgeFilter),
            typeof(AgeFilter),
            typeof(AgeFilterPicker),
            new PropertyMetadata(AgeFilter.Alle, OnSelectedAgeFilterChanged));

    public AgeFilter SelectedAgeFilter
    {
        get => (AgeFilter)GetValue(SelectedAgeFilterProperty);
        set => SetValue(SelectedAgeFilterProperty, value);
    }

    /// <summary>
    /// Event wenn sich die Auswahl aendert
    /// </summary>
    public event EventHandler? SelectionChanged;

    private static void OnSelectedAgeFilterChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AgeFilterPicker picker)
        {
            picker.UpdateSelectionText();
            picker.UpdateRadioButtons();
        }
    }

    private void RadioButton_Checked(object sender, RoutedEventArgs e)
    {
        if (sender is RadioButton radioButton && radioButton.Tag is string tagValue)
        {
            SelectedAgeFilter = tagValue switch
            {
                "Alle" => AgeFilter.Alle,
                "EinTag" => AgeFilter.EinTag,
                "EineWoche" => AgeFilter.EineWoche,
                "EinMonat" => AgeFilter.EinMonat,
                "EinJahr" => AgeFilter.EinJahr,
                _ => AgeFilter.Alle
            };

            SelectionChanged?.Invoke(this, EventArgs.Empty);
            PickerFlyout.Hide();
        }
    }

    private void UpdateSelectionText()
    {
        var text = SelectedAgeFilter switch
        {
            AgeFilter.Alle => "Älter als...",
            AgeFilter.EinTag => "1 Tag",
            AgeFilter.EineWoche => "1 Woche",
            AgeFilter.EinMonat => "1 Monat",
            AgeFilter.EinJahr => "1 Jahr",
            _ => "Älter als..."
        };

        SelectionText.Text = text;

        // Farbe anpassen: ausgewaehlt = OnSurface, nicht ausgewaehlt = OnSurfaceVariant
        SelectionText.Foreground = SelectedAgeFilter == AgeFilter.Alle
            ? (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceVariantBrush"]
            : (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
    }

    private void UpdateRadioButtons()
    {
        RadioAlle.IsChecked = SelectedAgeFilter == AgeFilter.Alle;
        RadioEinTag.IsChecked = SelectedAgeFilter == AgeFilter.EinTag;
        RadioEineWoche.IsChecked = SelectedAgeFilter == AgeFilter.EineWoche;
        RadioEinMonat.IsChecked = SelectedAgeFilter == AgeFilter.EinMonat;
        RadioEinJahr.IsChecked = SelectedAgeFilter == AgeFilter.EinJahr;
    }
}
