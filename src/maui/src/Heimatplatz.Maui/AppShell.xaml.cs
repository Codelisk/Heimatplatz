using System.Reflection;
using Heimatplatz.Maui.Features.Properties;
using Shiny;

namespace Heimatplatz.Maui;

public partial class AppShell : ShinyShell
{
    public AppShell()
    {
        InitializeComponent();

        // Standardweg fuer die Inseratserstellung: KI-Flow auf Android/iOS-Phones,
        // manuelle Erfassung auf allen anderen Geraeten
        Navigate.SetRoute(AddPropertyMenuItem, PropertyCreationRoutes.Default);

        SyncMenuItemIcons();

        VersionLabel.Text = $"Heimatplatz · Version {AppInfo.Current.VersionString}";
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
