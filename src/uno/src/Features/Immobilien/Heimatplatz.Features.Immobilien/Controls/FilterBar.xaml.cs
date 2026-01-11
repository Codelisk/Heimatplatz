using Heimatplatz.Core.ApiClient.Generated;
using Microsoft.UI.Xaml.Controls.Primitives;
using ICommand = System.Windows.Input.ICommand;

namespace Heimatplatz.Features.Immobilien.Controls;

public sealed partial class FilterBar : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty SelectedTypeProperty =
        DependencyProperty.Register(nameof(SelectedType), typeof(ImmobilienTyp?), typeof(FilterBar),
            new PropertyMetadata(null, OnSelectedTypeChanged));

    public static readonly DependencyProperty MinPriceProperty =
        DependencyProperty.Register(nameof(MinPrice), typeof(decimal), typeof(FilterBar),
            new PropertyMetadata(0m));

    public static readonly DependencyProperty MaxPriceProperty =
        DependencyProperty.Register(nameof(MaxPrice), typeof(decimal), typeof(FilterBar),
            new PropertyMetadata(5_000_000m, OnMaxPriceChanged));

    public static readonly DependencyProperty SearchLocationProperty =
        DependencyProperty.Register(nameof(SearchLocation), typeof(string), typeof(FilterBar),
            new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty MinAreaProperty =
        DependencyProperty.Register(nameof(MinArea), typeof(decimal), typeof(FilterBar),
            new PropertyMetadata(0m));

    public static readonly DependencyProperty FilterChangedCommandProperty =
        DependencyProperty.Register(nameof(FilterChangedCommand), typeof(ICommand), typeof(FilterBar),
            new PropertyMetadata(null));

    #endregion

    #region Properties

    public ImmobilienTyp? SelectedType
    {
        get => (ImmobilienTyp?)GetValue(SelectedTypeProperty);
        set => SetValue(SelectedTypeProperty, value);
    }

    public decimal MinPrice
    {
        get => (decimal)GetValue(MinPriceProperty);
        set => SetValue(MinPriceProperty, value);
    }

    public decimal MaxPrice
    {
        get => (decimal)GetValue(MaxPriceProperty);
        set => SetValue(MaxPriceProperty, value);
    }

    public string SearchLocation
    {
        get => (string)GetValue(SearchLocationProperty);
        set => SetValue(SearchLocationProperty, value);
    }

    public decimal MinArea
    {
        get => (decimal)GetValue(MinAreaProperty);
        set => SetValue(MinAreaProperty, value);
    }

    public ICommand? FilterChangedCommand
    {
        get => (ICommand?)GetValue(FilterChangedCommandProperty);
        set => SetValue(FilterChangedCommandProperty, value);
    }

    #endregion

    public FilterBar()
    {
        this.InitializeComponent();

        HouseToggle.Checked += OnHouseToggleChecked;
        LandToggle.Checked += OnLandToggleChecked;
        PriceSlider.ValueChanged += OnPriceSliderChanged;
        LocationSearchBox.TextChanged += OnLocationSearchChanged;
        MinAreaBox.TextChanged += OnMinAreaChanged;
    }

    private void OnHouseToggleChecked(object sender, RoutedEventArgs e)
    {
        LandToggle.IsChecked = false;
        HouseToggle.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        HouseToggle.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        LandToggle.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
        LandToggle.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
        SelectedType = ImmobilienTyp.Haus;
        NotifyFilterChanged();
    }

    private void OnLandToggleChecked(object sender, RoutedEventArgs e)
    {
        HouseToggle.IsChecked = false;
        LandToggle.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        LandToggle.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        HouseToggle.Background = new SolidColorBrush(Microsoft.UI.Colors.White);
        HouseToggle.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
        SelectedType = ImmobilienTyp.Grundstueck;
        NotifyFilterChanged();
    }

    private void OnPriceSliderChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        MaxPrice = (decimal)e.NewValue;
        UpdatePriceRangeText();
        NotifyFilterChanged();
    }

    private void OnLocationSearchChanged(object sender, TextChangedEventArgs e)
    {
        SearchLocation = LocationSearchBox.Text;
        NotifyFilterChanged();
    }

    private void OnMinAreaChanged(object sender, TextChangedEventArgs e)
    {
        if (decimal.TryParse(MinAreaBox.Text, out var area))
        {
            MinArea = area;
            NotifyFilterChanged();
        }
    }

    private void UpdatePriceRangeText()
    {
        var minText = MinPrice >= 1_000_000 ? $"\u20AC{MinPrice / 1_000_000:N1}M" : $"\u20AC{MinPrice / 1_000:N0}K";
        var maxText = MaxPrice >= 1_000_000 ? $"\u20AC{MaxPrice / 1_000_000:N1}M" : $"\u20AC{MaxPrice / 1_000:N0}K";
        PriceRangeText.Text = $"{minText} - {maxText}";
    }

    private void NotifyFilterChanged()
    {
        FilterChangedCommand?.Execute(null);
    }

    private static void OnSelectedTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterBar filterBar && e.NewValue is ImmobilienTyp type)
        {
            if (type == ImmobilienTyp.Haus)
            {
                filterBar.HouseToggle.IsChecked = true;
            }
            else if (type == ImmobilienTyp.Grundstueck)
            {
                filterBar.LandToggle.IsChecked = true;
            }
        }
    }

    private static void OnMaxPriceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterBar filterBar && e.NewValue is decimal price)
        {
            filterBar.PriceSlider.Value = (double)price;
            filterBar.UpdatePriceRangeText();
        }
    }
}
