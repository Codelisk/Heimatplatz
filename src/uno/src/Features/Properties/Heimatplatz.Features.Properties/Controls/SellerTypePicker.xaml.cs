using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// MultiSelect Picker fuer Anbietertypen (Privat, Makler, Portal)
/// Analog zum OrtPicker mit Button und Flyout
/// </summary>
public sealed partial class SellerTypePicker : UserControl
{
    public SellerTypePicker()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Liste der Anbietertypen
    /// </summary>
    public static readonly DependencyProperty SellerTypesProperty =
        DependencyProperty.Register(
            nameof(SellerTypes),
            typeof(IList<SellerTypeModel>),
            typeof(SellerTypePicker),
            new PropertyMetadata(null, OnSellerTypesChanged));

    public IList<SellerTypeModel>? SellerTypes
    {
        get => (IList<SellerTypeModel>?)GetValue(SellerTypesProperty);
        set => SetValue(SellerTypesProperty, value);
    }

    /// <summary>
    /// Event wenn sich die Auswahl aendert
    /// </summary>
    public event EventHandler? SelectionChanged;

    private static void OnSellerTypesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SellerTypePicker picker)
        {
            picker.UpdateSelectionText();
            picker.SubscribeToSelectionChanges();
        }
    }

    private void SubscribeToSelectionChanges()
    {
        if (SellerTypes == null) return;

        foreach (var sellerType in SellerTypes)
        {
            sellerType.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(SellerTypeModel.IsSelected))
                {
                    // Mindestens ein Typ muss ausgewaehlt bleiben
                    if (s is SellerTypeModel changedType && !changedType.IsSelected)
                    {
                        var selectedCount = SellerTypes.Count(st => st.IsSelected);
                        if (selectedCount == 0)
                        {
                            // Letzte Auswahl wieder aktivieren
                            changedType.IsSelected = true;
                            return;
                        }
                    }

                    UpdateSelectionText();
                    SelectionChanged?.Invoke(this, EventArgs.Empty);
                }
            };
        }
    }

    private void UpdateSelectionText()
    {
        if (SellerTypes == null)
        {
            SelectionText.Text = "Anbieter: Alle";
            return;
        }

        var selectedTypes = SellerTypes.Where(st => st.IsSelected).ToList();

        if (selectedTypes.Count == 0 || selectedTypes.Count == SellerTypes.Count)
        {
            SelectionText.Text = "Anbieter: Alle";
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
        }
        else if (selectedTypes.Count == 1)
        {
            SelectionText.Text = $"Anbieter: {selectedTypes[0].DisplayName}";
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
        }
        else
        {
            var names = string.Join(", ", selectedTypes.Select(st => st.DisplayName));
            SelectionText.Text = $"Anbieter: {names}";
            SelectionText.Foreground = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["OnSurfaceBrush"];
        }
    }

    /// <summary>
    /// Gibt die aktuellen Auswahl-Werte zurueck
    /// </summary>
    public (bool IsPrivateSelected, bool IsBrokerSelected, bool IsPortalSelected) GetSelection()
    {
        if (SellerTypes == null)
            return (true, true, true);

        return (
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Private)?.IsSelected ?? true,
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Broker)?.IsSelected ?? true,
            SellerTypes.FirstOrDefault(st => st.Type == SellerType.Portal)?.IsSelected ?? true
        );
    }
}
