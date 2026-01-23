using Heimatplatz.Events;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// MainPage mit NavigationView und Header Regions.
/// Handled ToggleNavigationPaneEvent vom AppHeaderLeft fuer den Hamburger Button.
/// Handled PageNavigatedEvent fuer MainHeader Navigation.
/// </summary>
public sealed partial class MainPage : Page,
    IEventHandler<ToggleNavigationPaneEvent>,
    IEventHandler<PageNavigatedEvent>
{
    public MainPage()
    {
        this.InitializeComponent();

        // Initial zu HeaderLeft, HeaderRight Regions navigieren
        Loaded += async (_, _) =>
        {
            var navigator = this.Navigator();
            if (navigator != null)
            {
                await navigator.NavigateRouteAsync(this, "./HeaderLeft/HeaderLeft");
                await navigator.NavigateRouteAsync(this, "./HeaderRight/HeaderRight");
            }

            // Home-Item auswählen um Navigation auszulösen
            if (NavView.MenuItems.Count > 0 && NavView.SelectedItem == null)
            {
                NavView.SelectedItem = NavView.MenuItems[0];
            }
        };
    }

    /// <summary>
    /// Handled das ToggleNavigationPaneEvent vom AppHeaderLeft (Hamburger Button)
    /// </summary>
    public new Task Handle(ToggleNavigationPaneEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Handled das PageNavigatedEvent - navigiert zur HeaderMain Region wenn MainHeaderViewModel gesetzt
    /// </summary>
    public new Task Handle(PageNavigatedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Ensure UI updates happen on UI thread
        var dispatcherQueue = DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(async () =>
            {
                await HandleMainHeaderNavigation(@event.MainHeaderViewModel);
            });
        }

        return Task.CompletedTask;
    }

    private Task HandleMainHeaderNavigation(Type? mainHeaderViewModel)
    {
        // TODO: HeaderMain Navigation temporär deaktiviert - überschreibt Content-Region
        // Die HeaderMain Region ist nur für Wide-Screens gedacht und wird später implementiert
        System.Diagnostics.Debug.WriteLine($"[MainPage] HeaderMain navigation skipped for: {mainHeaderViewModel?.Name ?? "null"}");
        return Task.CompletedTask;
    }
}
