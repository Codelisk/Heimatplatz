using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// Bezirk/Gemeinde Picker mit aufklappbaren Bezirken und Gemeinde-Checkboxen.
/// Unterstuetzt Multi-Select (Filter) und Single-Select (Formular) Modus.
/// </summary>
public sealed partial class OrtPicker : UserControl
{
    private bool _isSyncing;

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
    /// Bindable list of selected Gemeinde names (output property, multi-select)
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
    /// Single-Select Modus: Nur eine Gemeinde kann ausgewaehlt werden.
    /// Im Single-Select wird das Flyout nach Auswahl automatisch geschlossen.
    /// </summary>
    public static readonly DependencyProperty IsSingleSelectProperty =
        DependencyProperty.Register(
            nameof(IsSingleSelect),
            typeof(bool),
            typeof(OrtPicker),
            new PropertyMetadata(false));

    public bool IsSingleSelect
    {
        get => (bool)GetValue(IsSingleSelectProperty);
        set => SetValue(IsSingleSelectProperty, value);
    }

    /// <summary>
    /// Single-Select Output: Die ID der ausgewaehlten Gemeinde.
    /// </summary>
    public static readonly DependencyProperty SelectedGemeindeIdProperty =
        DependencyProperty.Register(
            nameof(SelectedGemeindeId),
            typeof(Guid?),
            typeof(OrtPicker),
            new PropertyMetadata(null, OnSelectedGemeindeIdChanged));

    public Guid? SelectedGemeindeId
    {
        get => (Guid?)GetValue(SelectedGemeindeIdProperty);
        set => SetValue(SelectedGemeindeIdProperty, value);
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
            picker.SubscribeToSelectionChanges();
            // Apply pre-set SelectedGemeindeId if available
            if (picker.IsSingleSelect && picker.SelectedGemeindeId.HasValue)
            {
                picker.ApplySelectedGemeindeId(picker.SelectedGemeindeId.Value);
            }
            picker.UpdateSelectionText();
        }
    }

    private static void OnSelectedGemeindeIdChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is OrtPicker picker && !picker._isSyncing && picker.Bezirke != null)
        {
            if (e.NewValue is Guid id)
            {
                picker.ApplySelectedGemeindeId(id);
            }
            else
            {
                picker.ClearAllSelections();
            }
            picker.UpdateSelectionText();
        }
    }

    private void ApplySelectedGemeindeId(Guid id)
    {
        if (Bezirke == null) return;

        _isSyncing = true;
        try
        {
            foreach (var bezirk in Bezirke)
            {
                foreach (var gemeinde in bezirk.Gemeinden)
                {
                    gemeinde.IsSelected = gemeinde.Id == id;
                }
            }
        }
        finally
        {
            _isSyncing = false;
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
                    if (e.PropertyName == nameof(GemeindeModel.IsSelected) && !_isSyncing)
                    {
                        if (IsSingleSelect && s is GemeindeModel selected && selected.IsSelected)
                        {
                            EnforceSingleSelect(selected);
                            UpdateSelectedGemeindeId(selected);
                            CloseFlyout();
                        }

                        UpdateSelectionText();
                        UpdateSelectedOrte();
                        SelectionChanged?.Invoke(this, EventArgs.Empty);
                    }
                };
            }
        }
    }

    private void EnforceSingleSelect(GemeindeModel selectedGemeinde)
    {
        if (Bezirke == null) return;

        _isSyncing = true;
        try
        {
            foreach (var bezirk in Bezirke)
            {
                foreach (var gemeinde in bezirk.Gemeinden)
                {
                    if (gemeinde != selectedGemeinde)
                    {
                        gemeinde.IsSelected = false;
                    }
                }
            }
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void UpdateSelectedGemeindeId(GemeindeModel gemeinde)
    {
        _isSyncing = true;
        try
        {
            SelectedGemeindeId = gemeinde.IsSelected ? gemeinde.Id : null;
        }
        finally
        {
            _isSyncing = false;
        }
    }

    private void CloseFlyout()
    {
        PickerFlyout.Hide();
    }

    private void ClearAllSelections()
    {
        if (Bezirke == null) return;

        _isSyncing = true;
        try
        {
            foreach (var bezirk in Bezirke)
            {
                foreach (var gemeinde in bezirk.Gemeinden)
                {
                    gemeinde.IsSelected = false;
                }
            }
        }
        finally
        {
            _isSyncing = false;
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
            var g = selectedGemeinden[0];
            SelectionText.Text = IsSingleSelect ? $"{g.Name} ({g.PostalCode})" : g.Name;
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
        ClearAllSelections();

        if (IsSingleSelect)
        {
            _isSyncing = true;
            try
            {
                SelectedGemeindeId = null;
            }
            finally
            {
                _isSyncing = false;
            }
        }

        UpdateSelectionText();
        UpdateSelectedOrte();
        SelectionChanged?.Invoke(this, EventArgs.Empty);
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
