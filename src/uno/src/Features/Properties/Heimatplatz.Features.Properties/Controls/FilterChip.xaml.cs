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
            new PropertyMetadata(false, OnIsSelectedChanged));

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
            chip.ChipButton.IsChecked = (bool)e.NewValue;
            chip.UpdateVisualState();
        }
    }

    private void OnChipClick(object sender, RoutedEventArgs e)
    {
        IsSelected = ChipButton.IsChecked ?? false;
        SelectionChanged?.Invoke(this, IsSelected);
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
