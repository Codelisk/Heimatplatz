namespace Heimatplatz.App.Controls;

/// <summary>
/// Gemeinsamer App-Header fuer alle Seiten
/// Enthaelt Logo und Auth-Bereich (Login/Register oder Profil-Menue)
/// </summary>
public sealed partial class AppHeader : UserControl
{
    public AppHeaderViewModel? ViewModel => DataContext as AppHeaderViewModel;

    /// <summary>
    /// Optionaler Content fuer den mittleren Bereich (z.B. Filter)
    /// </summary>
    public static readonly DependencyProperty HeaderContentProperty =
        DependencyProperty.Register(
            nameof(HeaderContent),
            typeof(object),
            typeof(AppHeader),
            new PropertyMetadata(null));

    public object? HeaderContent
    {
        get => GetValue(HeaderContentProperty);
        set => SetValue(HeaderContentProperty, value);
    }

    public AppHeader()
    {
        this.InitializeComponent();
    }
}
