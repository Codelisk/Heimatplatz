using Heimatplatz.App.Controls;
using Heimatplatz.Events;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Controls;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Pages;
using Windows.UI.Core;

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
    private IAuthService? _authService;
    private PageType _currentPageType = PageType.Home;

    public MainPage()
    {
        this.InitializeComponent();

        // Handle hardware back button (Android, browser)
        SystemNavigationManager.GetForCurrentView().BackRequested += OnBackRequested;

        // Initial zu HeaderLeft, HeaderRight Regions navigieren und Home laden
        Loaded += async (_, _) =>
        {
            // HeaderMain eager vorab fuellen, damit der Desktop-Filter bei Widest
            // sofort sichtbar ist (vor dem ersten PageNavigatedEvent).
            EnsureHeaderMainContent();

            // HeaderLeft/Right Content programmatisch befuellen (Region-basierte Navigation
            // ueber "./menue" / "./konto" laed den View nicht zuverlaessig in die ContentControls,
            // weil das initiale `await NavigateAsync<Shell>` haengt und der Navigator dadurch
            // die regions nicht aufloest. Wir setzen die Inhalte direkt - so funktioniert auch
            // der Back-Button auf Detail-Pages).
            EnsureHeaderRegionContents();

            var navigator = this.Navigator();
            if (navigator != null)
            {
                // Explizit zur Home-Route navigieren (SelectedItem setzen reicht nicht)
                var navViewNavigator = NavView.Navigator();
                if (navViewNavigator != null)
                {
                    await navViewNavigator.NavigateRouteAsync(NavView, "hauptseite");
                }
            }

            // Subscribe to auth changes to update PaneDisplayMode
            if (Application.Current is App app && app.Services != null)
            {
                _authService = app.Services.GetService<IAuthService>();
                if (_authService != null)
                {
                    _authService.AuthenticationStateChanged += OnAuthStateChanged;
                    UpdatePaneDisplayMode(_authService.IsAuthenticated);
                }
            }
        };

        SizeChanged += (_, _) => UpdatePaneDisplayMode(_authService?.IsAuthenticated ?? false);
    }

    private void OnAuthStateChanged(object? sender, bool isAuthenticated)
    {
        DispatcherQueue?.TryEnqueue(() => UpdatePaneDisplayMode(isAuthenticated));
    }

    private void UpdatePaneDisplayMode(bool isAuthenticated)
    {
        // Only show full pane (Left) when authenticated AND wide screen
        // Otherwise always use LeftMinimal
        if (isAuthenticated && ActualWidth >= 600)
        {
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        }
        else
        {
            NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
        }
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
                await navViewNavigator.NavigateRouteAsync(NavView, "hauptseite");
            }
        });

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles the hardware back button (Android, browser).
    /// Navigates back to Home when on a Detail or Form page.
    /// </summary>
    private void OnBackRequested(object? sender, BackRequestedEventArgs e)
    {
        if (_currentPageType is PageType.Detail or PageType.Form)
        {
            e.Handled = true;
            DispatcherQueue?.TryEnqueue(async () =>
            {
                var navViewNavigator = NavView.Navigator();
                if (navViewNavigator != null)
                {
                    System.Diagnostics.Debug.WriteLine("[MainPage] Hardware back - Navigating to Home");
                    await navViewNavigator.NavigateRouteAsync(NavView, "hauptseite");
                }
            });
        }
    }

    /// <summary>
    /// Handled das PageNavigatedEvent - navigiert zur HeaderMain Region wenn MainHeaderViewModel gesetzt
    /// </summary>
    public new Task Handle(PageNavigatedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        _currentPageType = @event.PageType;

        // Ensure UI updates happen on UI thread
        var dispatcherQueue = DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(async () =>
            {
                // Hide pane on Detail/Form pages, restore on navigation pages
                if (_currentPageType is PageType.Detail or PageType.Form)
                {
                    NavView.IsPaneOpen = false;
                    NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftMinimal;
                    NavView.IsPaneVisible = false;
                }
                else if (_authService != null)
                {
                    NavView.IsPaneVisible = _authService.IsAuthenticated;
                    UpdatePaneDisplayMode(_authService.IsAuthenticated);
                }

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
            // Use NavigationContent's navigator to navigate within the content region directly.
            // NavView.Navigator() only works for routes that have a corresponding NavigationViewItem.
            // Routes without a NavigationViewItem (e.g. UserProfile) would bubble up to the Shell,
            // replacing MainPage entirely and losing header/NavigationView.
            var contentNavigator = NavigationContent.Navigator();
            if (contentNavigator != null)
            {
                System.Diagnostics.Debug.WriteLine($"[MainPage] Navigating to {@event.Route} in Content region");
                await contentNavigator.NavigateRouteAsync(NavigationContent, @event.Route);
            }
        });

        return Task.CompletedTask;
    }

    private Task HandleMainHeaderNavigation(Type? mainHeaderViewModel)
    {
        // HeaderMain Content direkt setzen (nicht über Navigation, da das Content-Region überschreibt)
        if (mainHeaderViewModel == typeof(HomeFilterBarViewModel))
        {
            EnsureHeaderMainContent();
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

    /// <summary>
    /// Stellt sicher, dass die HomeFilterBar als HeaderMain Content gesetzt ist.
    /// Wird verwendet, falls der HeaderMain auf Tablet/Desktop irgendwann reaktiviert
    /// werden soll. Aktuell ist HeaderMain in MainPage.xaml auf Collapsed gesetzt,
    /// und der Filter wird inline in HomePage angezeigt - das funktioniert ohne
    /// Abhaengigkeit von <c>App.Host</c>, das beim initialen Start lange null bleibt
    /// (NavigateAsync&lt;Shell&gt; kehrt erst nach vollstaendiger Navigation zurueck).
    /// </summary>
    private void EnsureHeaderMainContent()
    {
        if (HeaderMainContent.Content is HomeFilterBar)
            return;

        if (Application.Current is not App app || app.Services == null)
            return;

        var filterBar = new HomeFilterBar
        {
            DataContext = app.Services.GetRequiredService<HomeFilterBarViewModel>()
        };
        HeaderMainContent.Content = filterBar;
    }

    /// <summary>
    /// Setzt AppHeaderLeft (Hamburger/Back/Logo) und AppHeaderRight (Auth-Buttons)
    /// direkt als Content der entsprechenden ContentControls. Bypasses die Uno
    /// Region-Navigation, die zur initialen Loaded-Zeit nicht zuverlaessig laeuft.
    /// </summary>
    private void EnsureHeaderRegionContents()
    {
        if (Application.Current is not App app || app.Services == null)
            return;

        if (HeaderLeftContent.Content is not AppHeaderLeft)
        {
            HeaderLeftContent.Content = new AppHeaderLeft
            {
                DataContext = app.Services.GetRequiredService<AppHeaderLeftViewModel>()
            };
        }

        if (HeaderRightContent.Content is not AppHeaderRight)
        {
            HeaderRightContent.Content = new AppHeaderRight
            {
                DataContext = app.Services.GetRequiredService<AppHeaderRightViewModel>()
            };
        }
    }

}
