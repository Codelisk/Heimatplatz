using System.Globalization;
using System.Windows.Input;

namespace Heimatplatz.Core.Controls.Controls;

public sealed partial class PropertyCard : UserControl
{
    #region Dependency Properties

    public static readonly DependencyProperty ImageUrlProperty =
        DependencyProperty.Register(nameof(ImageUrl), typeof(string), typeof(PropertyCard),
            new PropertyMetadata(null, OnImageUrlChanged));

    public static readonly DependencyProperty LocationProperty =
        DependencyProperty.Register(nameof(Location), typeof(string), typeof(PropertyCard),
            new PropertyMetadata(string.Empty, OnLocationChanged));

    public static readonly DependencyProperty PriceProperty =
        DependencyProperty.Register(nameof(Price), typeof(decimal), typeof(PropertyCard),
            new PropertyMetadata(0m, OnPriceChanged));

    public static readonly DependencyProperty AreaProperty =
        DependencyProperty.Register(nameof(Area), typeof(decimal), typeof(PropertyCard),
            new PropertyMetadata(0m, OnAreaChanged));

    public static readonly DependencyProperty ExtraInfoProperty =
        DependencyProperty.Register(nameof(ExtraInfo), typeof(string), typeof(PropertyCard),
            new PropertyMetadata(string.Empty, OnExtraInfoChanged));

    public static readonly DependencyProperty CommandProperty =
        DependencyProperty.Register(nameof(Command), typeof(ICommand), typeof(PropertyCard),
            new PropertyMetadata(null));

    public static readonly DependencyProperty CommandParameterProperty =
        DependencyProperty.Register(nameof(CommandParameter), typeof(object), typeof(PropertyCard),
            new PropertyMetadata(null));

    #endregion

    #region Properties

    public string? ImageUrl
    {
        get => (string?)GetValue(ImageUrlProperty);
        set => SetValue(ImageUrlProperty, value);
    }

    public string Location
    {
        get => (string)GetValue(LocationProperty);
        set => SetValue(LocationProperty, value);
    }

    public decimal Price
    {
        get => (decimal)GetValue(PriceProperty);
        set => SetValue(PriceProperty, value);
    }

    public decimal Area
    {
        get => (decimal)GetValue(AreaProperty);
        set => SetValue(AreaProperty, value);
    }

    public string ExtraInfo
    {
        get => (string)GetValue(ExtraInfoProperty);
        set => SetValue(ExtraInfoProperty, value);
    }

    public ICommand? Command
    {
        get => (ICommand?)GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    #endregion

    public PropertyCard()
    {
        this.InitializeComponent();
        this.PointerPressed += OnPointerPressed;
    }

    private void OnPointerPressed(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        Command?.Execute(CommandParameter);
    }

    #region Property Changed Handlers

    private static void OnImageUrlChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            var url = e.NewValue as string;
            if (!string.IsNullOrEmpty(url))
            {
                card.PropertyImage.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(new Uri(url));
                card.PropertyImage.Visibility = Visibility.Visible;
                card.ImagePlaceholder.Visibility = Visibility.Collapsed;
            }
            else
            {
                card.PropertyImage.Visibility = Visibility.Collapsed;
                card.ImagePlaceholder.Visibility = Visibility.Visible;
            }
        }
    }

    private static void OnLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            card.LocationText.Text = (e.NewValue as string)?.ToUpperInvariant() ?? string.Empty;
        }
    }

    private static void OnPriceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card && e.NewValue is decimal price)
        {
            card.PriceText.Text = FormatPrice(price);
        }
    }

    private static void OnAreaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card && e.NewValue is decimal area)
        {
            card.AreaText.Text = $"{area:N0}m\u00B2";
        }
    }

    private static void OnExtraInfoChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PropertyCard card)
        {
            card.ExtraInfoText.Text = e.NewValue as string ?? string.Empty;
        }
    }

    #endregion

    private static string FormatPrice(decimal price)
    {
        if (price >= 1_000_000)
        {
            return $"\u20AC{price / 1_000_000:N2}M";
        }
        else if (price >= 1_000)
        {
            return $"\u20AC{price / 1_000:N0}K";
        }
        return $"\u20AC{price:N0}";
    }
}
