using Heimatplatz.Core.ApiClient.Generated;
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

        HouseToggle.Click += OnHouseToggleClick;
        LandToggle.Click += OnLandToggleClick;
        LocationSearchBox.TextChanged += OnLocationSearchChanged;
        MinAreaBox.TextChanged += OnMinAreaChanged;
    }

    private void OnHouseToggleClick(object sender, RoutedEventArgs e)
    {
        HouseToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        HouseToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        LandToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        LandToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
        SelectedType = ImmobilienTyp.Haus;
        NotifyFilterChanged();
    }

    private void OnLandToggleClick(object sender, RoutedEventArgs e)
    {
        LandToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
        LandToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
        HouseToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
        HouseToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
        SelectedType = ImmobilienTyp.Grundstueck;
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
                filterBar.HouseToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
                filterBar.HouseToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                filterBar.LandToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                filterBar.LandToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
            }
            else if (type == ImmobilienTyp.Grundstueck)
            {
                filterBar.LandToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Black);
                filterBar.LandToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.White);
                filterBar.HouseToggleBg.Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent);
                filterBar.HouseToggleText.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black);
            }
        }
    }

    private static void OnMaxPriceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is FilterBar filterBar && e.NewValue is decimal)
        {
            filterBar.UpdatePriceRangeText();
        }
    }
}
