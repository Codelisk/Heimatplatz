using System.Reflection;
using Heimatplatz.Maui.Features.Properties;
using Shiny;

namespace Heimatplatz.Maui;

public partial class AppShell : ShinyShell
{
    public AppShell()
    {
        InitializeComponent();

        // Gemeinsames Template fuer FlyoutItems und MenuItems (einheitliche Optik).
        // Zuweisung hier statt im XAML-Attribut, weil StaticResource auf dem Root-Element
        // vor dem Parsen von Shell.Resources nicht aufloesbar waere.
        var flyoutEntryTemplate = (DataTemplate)Resources["FlyoutEntryTemplate"];
        ItemTemplate = flyoutEntryTemplate;
        MenuItemTemplate = flyoutEntryTemplate;

        // Standardweg fuer die Inseratserstellung: KI-Flow auf Android/iOS-Phones,
        // manuelle Erfassung auf allen anderen Geraeten
        Navigate.SetRoute(AddPropertyMenuItem, PropertyCreationRoutes.Default);

        // MenuItems schliessen das Flyout - anders als FlyoutItems - nicht automatisch
        AddPropertyMenuItem.Clicked += (_, _) => FlyoutIsPresented = false;

#if DEBUG
        // Debug-Werkzeuge (z.B. API-Umschalter) nur in Entwicklungs-Builds im Flyout
        Items.Add(new ShellContent
        {
            Title = "Debug",
            Icon = "icon_bug.png",
            Route = "Debug",
            ContentTemplate = new DataTemplate(typeof(Features.Debug.Presentation.DebugPage))
        });
#endif

        SyncMenuItemIcons();

        VersionLabel.Text = $"Heimatplatz · Version {AppInfo.Current.VersionString}";
    }

    /// <summary>
    /// Footer-Link Impressum: Flyout schliessen und Seite auf den Navigationsstack pushen
    /// </summary>
    private async void OnImprintTapped(object? sender, TappedEventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("Imprint");
    }

    /// <summary>
    /// Footer-Link Datenschutz: Flyout schliessen und Seite auf den Navigationsstack pushen
    /// </summary>
    private async void OnPrivacyPolicyTapped(object? sender, TappedEventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("PrivacyPolicy");
    }

    /// <summary>
    /// Oeffnet aus dem Flyout dieselbe Filterseite wie das Toolbar-Symbol der Startseite.
    /// </summary>
    private async void OnFilterSettingsTapped(object? sender, EventArgs e)
    {
        FlyoutIsPresented = false;
        await GoToAsync("FilterSettings");
    }

    /// <summary>
    /// Workaround: Das interne MenuShellItem uebernimmt IconImageSource des MenuItems
    /// nicht zuverlaessig in Icon/FlyoutIcon - daher explizit nachziehen, damit das
    /// MenuItemTemplate ({Binding Icon}) die Icons anzeigen kann.
    /// </summary>
    private void SyncMenuItemIcons()
    {
        foreach (var item in Items)
        {
            if (item.Icon is not null || item.GetType().Name != "MenuShellItem")
                continue;

            var menuItem = item.GetType()
                .GetProperty("MenuItem", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)?
                .GetValue(item) as MenuItem;

            if (menuItem?.IconImageSource is not null)
                item.Icon = menuItem.IconImageSource;
        }
    }
}
