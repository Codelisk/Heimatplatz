using Heimatplatz.Events;
using Heimatplatz.Features.Properties.Controls;
using Microsoft.Extensions.DependencyInjection;
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
/// Handled NavigateBackInContentEvent fuer Zurueck-Navigation in der Content-Region.
/// </summary>
public sealed partial class MainPage : Page,
    IEventHandler<ToggleNavigationPaneEvent>,
    IEventHandler<PageNavigatedEvent>,
    IEventHandler<NavigateBackInContentEvent>,
    IEventHandler<NavigateToRouteInContentEvent>
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
    /// Handled das NavigateBackInContentEvent - navigiert zurueck in der Content-Region
    /// </summary>
    public new Task Handle(NavigateBackInContentEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        var dispatcherQueue = DispatcherQueue;
        dispatcherQueue?.TryEnqueue(async () =>
        {
            // Navigate back to Home within the NavigationView
            // Using NavigateRouteAsync instead of NavigateBackAsync to stay within the Main region
            var navViewNavigator = NavView.Navigator();
            if (navViewNavigator != null)
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] Navigating to Home in Content region");
                await navViewNavigator.NavigateRouteAsync(NavView, "Home");
            }
        });

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

    /// <summary>
    /// Handled das NavigateToRouteInContentEvent - navigiert zu einer Route in der Content-Region.
    /// Ermoeglicht Controls ausserhalb der NavigationView (z.B. HeaderRight) die Navigation
    /// innerhalb der Content-Region auszuloesen.
    /// </summary>
    public new Task Handle(NavigateToRouteInContentEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        var dispatcherQueue = DispatcherQueue;
        dispatcherQueue?.TryEnqueue(async () =>
        {
            var navViewNavigator = NavView.Navigator();
            if (navViewNavigator != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Navigating to {@event.Route} in Content region");
                await navViewNavigator.NavigateRouteAsync(NavView, @event.Route);
            }
        });

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
                var filterBar = new HomeFilterBar();
                // ViewModel aus DI-Container holen und als DataContext setzen
                if (Application.Current is App app && app.Services != null)
                {
                    filterBar.DataContext = app.Services.GetRequiredService<HomeFilterBarViewModel>();
                }
                HeaderMainContent.Content = filterBar;
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
