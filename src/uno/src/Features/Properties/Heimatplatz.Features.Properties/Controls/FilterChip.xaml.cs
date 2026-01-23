using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace Heimatplatz.Features.Properties.Controls;

/// <summary>
/// FilterChip - Toggle-Button fuer Filteroptionen
/// </summary>
public sealed partial class FilterChip : UserControl
{
    public FilterChip()
    {
        this.InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Initialisiere Zustand nach dem Laden (wenn Binding bereits gesetzt wurde)
        ChipButton.IsChecked = IsSelected;
        ChipLabel.Text = Label ?? string.Empty;
        UpdateVisualState();
    }

    /// <summary>
    /// Label-Text des Chips
    /// </summary>
    public static readonly DependencyProperty LabelProperty =
        DependencyProperty.Register(
            nameof(Label),
            typeof(string),
            typeof(FilterChip),
            new PropertyMetadata(string.Empty, OnLabelChanged));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    /// <summary>
    /// Ob der Chip ausgewaehlt ist
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty =
        DependencyProperty.Register(
            nameof(IsSelected),
            typeof(bool),
            typeof(FilterChip),
            new PropertyMetadata(true, OnIsSelectedChanged));

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    /// <summary>
    /// Event wenn Selektion sich aendert
    /// </summary>
    public event EventHandler<bool>? SelectionChanged;

    private static void OnLabelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterChip chip)
        {
            chip.ChipLabel.Text = e.NewValue as string ?? string.Empty;
        }
    }

    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterChip chip)
        {
            // Immer den Button-Zustand synchronisieren, auch wenn vom ViewModel geändert
            var newValue = (bool)e.NewValue;
            if (chip.ChipButton.IsChecked != newValue)
            {
                chip.ChipButton.IsChecked = newValue;
            }
            chip.UpdateVisualState();
        }
    }

    private void OnChipClick(object sender, RoutedEventArgs e)
    {
        var newValue = ChipButton.IsChecked ?? false;
        IsSelected = newValue;
        SelectionChanged?.Invoke(this, newValue);

        // Nach dem Setzen: Prüfe ob das ViewModel den Wert zurückgesetzt hat
        // (z.B. wenn mindestens ein Filter aktiv bleiben muss)
        DispatcherQueue.TryEnqueue(() =>
        {
            if (ChipButton.IsChecked != IsSelected)
            {
                ChipButton.IsChecked = IsSelected;
                UpdateVisualState();
            }
        });
    }

    private void UpdateVisualState()
    {
        if (IsSelected)
        {
            ChipButton.Background = (Brush)Application.Current.Resources["ChipSelectedBackgroundBrush"];
            ChipButton.Foreground = (Brush)Application.Current.Resources["ChipSelectedForegroundBrush"];
            ChipButton.BorderThickness = new Thickness(0);
        }
        else
        {
            ChipButton.Background = (Brush)Application.Current.Resources["ChipUnselectedBackgroundBrush"];
            ChipButton.Foreground = (Brush)Application.Current.Resources["ChipUnselectedForegroundBrush"];
            ChipButton.BorderThickness = new Thickness(1);
        }
    }
}
