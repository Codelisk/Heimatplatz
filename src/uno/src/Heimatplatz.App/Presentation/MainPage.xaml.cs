using Heimatplatz.Events;
using Heimatplatz.Features.Properties.Controls;
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

        // Initial zu HeaderLeft, HeaderRight Regions navigieren und Home laden
        Loaded += async (_, _) =>
        {
            var navigator = this.Navigator();
            if (navigator != null)
            {
                await navigator.NavigateRouteAsync(this, "./HeaderLeft/HeaderLeft");
                await navigator.NavigateRouteAsync(this, "./HeaderRight/HeaderRight");

                // Explizit zur Home-Route navigieren (SelectedItem setzen reicht nicht)
                var navViewNavigator = NavView.Navigator();
                if (navViewNavigator != null)
                {
                    await navViewNavigator.NavigateRouteAsync(NavView, "Home");
                }
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
        // HeaderMain Content direkt setzen (nicht über Navigation, da das Content-Region überschreibt)
        if (mainHeaderViewModel == typeof(HomeFilterBarViewModel))
        {
            // HomeFilterBar nur setzen wenn noch nicht vorhanden
            if (HeaderMainContent.Content is not HomeFilterBar)
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] Setting HeaderMain content to HomeFilterBar");
                HeaderMainContent.Content = new HomeFilterBar();
            }
        }
        else
        {
            // HeaderMain leeren wenn kein ViewModel
            if (HeaderMainContent.Content != null)
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] Clearing HeaderMain content");
                HeaderMainContent.Content = null;
            }
        }

        return Task.CompletedTask;
    }
}
