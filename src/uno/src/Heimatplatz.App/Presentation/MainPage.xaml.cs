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
    private Type? _currentMainHeaderViewModel;

    public MainPage()
    {
        this.InitializeComponent();

        // Initial zu HeaderLeft und HeaderRight Regions navigieren - DataContext wird automatisch via ViewMap gesetzt
        Loaded += async (_, _) =>
        {
            var navigator = this.Navigator();
            if (navigator != null)
            {
                await navigator.NavigateRouteAsync(this, "./HeaderLeft/HeaderLeft");
                await navigator.NavigateRouteAsync(this, "./HeaderRight/HeaderRight");
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

    private async Task HandleMainHeaderNavigation(Type? mainHeaderViewModel)
    {
        // Skip if same ViewModel type (avoid unnecessary navigation)
        if (_currentMainHeaderViewModel == mainHeaderViewModel)
            return;

        _currentMainHeaderViewModel = mainHeaderViewModel;

        var navigator = this.Navigator();
        if (navigator == null)
            return;

        if (mainHeaderViewModel != null)
        {
            // Navigate to HeaderMain region with the specified ViewModel
            // Currently we only support HomeFilterBarViewModel
            if (mainHeaderViewModel == typeof(HomeFilterBarViewModel))
            {
                System.Diagnostics.Debug.WriteLine("[MainPage] Navigating to HeaderMain with HomeFilterBar");
                await navigator.NavigateRouteAsync(this, "./HeaderMain/HeaderMain");
            }
        }
        else
        {
            // Clear HeaderMain region by navigating to empty
            // Note: Uno Navigation doesn't have a clear method, so we leave it as is
            // The region will just keep the last content (hidden on non-Wide screens anyway)
            System.Diagnostics.Debug.WriteLine("[MainPage] MainHeaderViewModel is null - HeaderMain stays as is");
        }
    }
}
