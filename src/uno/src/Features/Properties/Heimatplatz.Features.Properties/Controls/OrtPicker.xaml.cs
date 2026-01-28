using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// Hierarchischer Ort-Picker mit Bundeslaendern, Bezirken und Gemeinden
/// </summary>
public sealed partial class OrtPicker : UserControl
{
    public OrtPicker()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Liste der Bundeslaender mit Bezirken und Gemeinden
    /// </summary>
    public static readonly DependencyProperty BundeslaenderProperty =
        DependencyProperty.Register(
            nameof(Bundeslaender),
            typeof(IList<BundeslandModel>),
            typeof(OrtPicker),
            new PropertyMetadata(null, OnBundeslaenderChanged));

    public IList<BundeslandModel>? Bundeslaender
    {
        get => (IList<BundeslandModel>?)GetValue(BundeslaenderProperty);
        set => SetValue(BundeslaenderProperty, value);
    }

    /// <summary>
    /// Bindable list of selected Gemeinde names (output property)
    /// </summary>
    public static readonly DependencyProperty SelectedOrteProperty =
        DependencyProperty.Register(
            nameof(SelectedOrte),
            typeof(List<string>),
            typeof(OrtPicker),
            new PropertyMetadata(new List<string>()));

    public List<string> SelectedOrte
    {
        get => (List<string>)GetValue(SelectedOrteProperty);
        set => SetValue(SelectedOrteProperty, value);
    }

    /// <summary>
    /// Event wenn sich die Auswahl aendert
    /// </summary>
    public event EventHandler? SelectionChanged;

    /// <summary>
    /// Zeigt Loading-State an
    /// </summary>
    public void SetLoading(bool isLoading)
    {
        LoadingRing.IsActive = isLoading;
        LoadingRing.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
    }

    private static void OnBundeslaenderChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OrtPicker picker)
        {
            picker.UpdateSelectionText();
            picker.SubscribeToSelectionChanges();
        }
    }

    private void SubscribeToSelectionChanges()
    {
        if (Bundeslaender == null) return;

        foreach (var bundesland in Bundeslaender)
        {
            foreach (var bezirk in bundesland.Bezirke)
            {
                foreach (var gemeinde in bezirk.Gemeinden)
                {
                    gemeinde.PropertyChanged += (s, e) =>
                    {
                        if (e.PropertyName == nameof(GemeindeModel.IsSelected))
                        {
                            UpdateSelectionText();
                            UpdateSelectedOrte();
                            SelectionChanged?.Invoke(this, EventArgs.Empty);
                        }
                    };
                }
            }
        }
    }

    private void UpdateSelectedOrte()
    {
        SelectedOrte = GetSelectedOrte();
    }

    private void UpdateSelectionText()
    {
        if (Bundeslaender == null)
        {
            SelectionText.Text = "Ort auswählen";
            return;
        }

        var selectedGemeinden = Bundeslaender
            .SelectMany(bl => bl.Bezirke)
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.IsSelected)
            .ToList();

        if (selectedGemeinden.Count == 0)
        {
            SelectionText.Text = "Ort auswählen";
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceVariantBrush"];
        }
        else if (selectedGemeinden.Count == 1)
        {
            SelectionText.Text = selectedGemeinden[0].Name;
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
        }
        else
        {
            SelectionText.Text = $"{selectedGemeinden.Count} Orte ausgewählt";
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
        }
    }

    private void BundeslandHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BundeslandModel bundesland)
        {
            bundesland.IsExpanded = !bundesland.IsExpanded;
        }
    }

    private void BezirkHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BezirkModel bezirk)
        {
            bezirk.IsExpanded = !bezirk.IsExpanded;
        }
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (Bundeslaender == null) return;

        foreach (var bundesland in Bundeslaender)
        {
            foreach (var bezirk in bundesland.Bezirke)
            {
                foreach (var gemeinde in bezirk.Gemeinden)
                {
                    gemeinde.IsSelected = false;
                }
            }
        }
    }

    /// <summary>
    /// Gibt die Liste der ausgewaehlten Gemeindenamen zurueck
    /// </summary>
    public List<string> GetSelectedOrte()
    {
        if (Bundeslaender == null) return new List<string>();

        return Bundeslaender
            .SelectMany(bl => bl.Bezirke)
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.IsSelected)
            .Select(g => g.Name)
            .ToList();
    }
}
