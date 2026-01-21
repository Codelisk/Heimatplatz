using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Gemeinsamer App-Header fuer alle Seiten
/// Enthaelt Logo und Auth-Bereich (Login/Register oder Profil-Menue)
/// </summary>
public sealed partial class AppHeader : UserControl
{
    public AppHeaderViewModel ViewModel { get; }

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
        // ViewModel via DI holen - App.Current als Heimatplatz.App.App casten
        var app = (Heimatplatz.App.App)Application.Current;
        var authService = app.Host!.Services.GetRequiredService<IAuthService>();
        ViewModel = new AppHeaderViewModel(authService);

        this.InitializeComponent();

        // Logout Command binden (x:Bind funktioniert nicht in Resources)
        if (Resources.TryGetValue("ProfileMenuFlyout", out var flyoutResource) && flyoutResource is MenuFlyout menuFlyout)
        {
            foreach (var item in menuFlyout.Items)
            {
                if (item is MenuFlyoutItem menuItem && menuItem.Name == "LogoutMenuItem")
                {
                    menuItem.Command = ViewModel.LogoutCommand;
                    break;
                }
            }
        }
    }
}
