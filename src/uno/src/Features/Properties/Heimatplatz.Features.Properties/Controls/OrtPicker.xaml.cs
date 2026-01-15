using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// Hierarchischer Ort-Picker mit Bezirken und Orten
/// </summary>
public sealed partial class OrtPicker : UserControl
{
    public OrtPicker()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Liste der Bezirke mit Orten
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
    /// Bindable list of selected Ort names (output property)
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
    /// Event wenn sich die Auswahl ändert
    /// </summary>
    public event EventHandler? SelectionChanged;

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
            foreach (var ort in bezirk.Orte)
            {
                ort.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(OrtModel.IsSelected))
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

        var selectedOrte = Bezirke
            .SelectMany(b => b.Orte)
            .Where(o => o.IsSelected)
            .ToList();

        if (selectedOrte.Count == 0)
        {
            SelectionText.Text = "Ort auswählen";
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceVariantBrush"];
        }
        else if (selectedOrte.Count == 1)
        {
            SelectionText.Text = selectedOrte[0].Name;
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
        }
        else
        {
            SelectionText.Text = $"{selectedOrte.Count} Orte ausgewählt";
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
            foreach (var ort in bezirk.Orte)
            {
                ort.IsSelected = false;
            }
        }
    }

    /// <summary>
    /// Gibt die Liste der ausgewählten Ortsnamen zurück
    /// </summary>
    public List<string> GetSelectedOrte()
    {
        if (Bezirke == null) return new List<string>();

        return Bezirke
            .SelectMany(b => b.Orte)
            .Where(o => o.IsSelected)
            .Select(o => o.Name)
            .ToList();
    }
}
