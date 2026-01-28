using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// Bezirk/Gemeinde Picker mit aufklappbaren Bezirken und Gemeinde-Checkboxen
/// </summary>
public sealed partial class OrtPicker : UserControl
{
    public OrtPicker()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Liste der Bezirke mit Gemeinden
    /// </summary>
    public static readonly DependencyProperty BezirkeProperty =
        DependencyProperty.Register(
            nameof(Bezirke),
            typeof(IList<BezirkModel>),
            typeof(OrtPicker),
            new PropertyMetadata(null, OnBezirkeChanged));

    public IList<BezirkModel>? Bezirke
    {
        get => (IList<BezirkModel>?)GetValue(BezirkeProperty);
        set => SetValue(BezirkeProperty, value);
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

    private static void OnBezirkeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OrtPicker picker)
        {
            picker.UpdateSelectionText();
            picker.SubscribeToSelectionChanges();
        }
    }

    private void SubscribeToSelectionChanges()
    {
        if (Bezirke == null) return;

        foreach (var bezirk in Bezirke)
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

    private void UpdateSelectedOrte()
    {
        SelectedOrte = GetSelectedOrte();
    }

    private void UpdateSelectionText()
    {
        if (Bezirke == null)
        {
            SelectionText.Text = "Ort auswählen";
            return;
        }

        var selectedGemeinden = Bezirke
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

    private void BezirkHeader_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is BezirkModel bezirk)
        {
            bezirk.IsExpanded = !bezirk.IsExpanded;
        }
    }

    private void ClearAllButton_Click(object sender, RoutedEventArgs e)
    {
        if (Bezirke == null) return;

        foreach (var bezirk in Bezirke)
        {
            foreach (var gemeinde in bezirk.Gemeinden)
            {
                gemeinde.IsSelected = false;
            }
        }
    }

    /// <summary>
    /// Gibt die Liste der ausgewaehlten Gemeindenamen zurueck
    /// </summary>
    public List<string> GetSelectedOrte()
    {
        if (Bezirke == null) return new List<string>();

        return Bezirke
            .SelectMany(b => b.Gemeinden)
            .Where(g => g.IsSelected)
            .Select(g => g.Name)
            .ToList();
    }
}
