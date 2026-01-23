using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Events;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.App.Controls;

/// <summary>
/// ViewModel fuer den linken Header-Bereich.
/// Reagiert auf PageNavigatedEvent und zeigt:
/// - Hamburger-Button (Home, List, Settings) oder Zurueck-Button (Detail, Form)
/// - Aktuellen Seitentitel
/// </summary>
public partial class AppHeaderLeftViewModel : ObservableObject,
    IEventHandler<PageNavigatedEvent>,
    IEventHandler<ToggleNavigationPaneEvent>
{
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILogger<AppHeaderLeftViewModel> _logger;

    /// <summary>
    /// Current page type - determines which button to show
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsMenuMode))]
    [NotifyPropertyChangedFor(nameof(IsBackMode))]
    private PageType _currentPageType = PageType.Home;

    /// <summary>
    /// Current page title
    /// </summary>
    [ObservableProperty]
    private string _currentTitle = "HEIMATPLATZ";

    /// <summary>
    /// Visibility for hamburger button (Menu mode: Home, List, Settings)
    /// </summary>
    public Visibility IsMenuMode => CurrentPageType switch
    {
        PageType.Home => Visibility.Visible,
        PageType.List => Visibility.Visible,
        PageType.Settings => Visibility.Visible,
        _ => Visibility.Collapsed
    };

    /// <summary>
    /// Visibility for back button (Back mode: Detail, Form)
    /// </summary>
    public Visibility IsBackMode => CurrentPageType switch
    {
        PageType.Detail => Visibility.Visible,
        PageType.Form => Visibility.Visible,
        _ => Visibility.Collapsed
    };

    public AppHeaderLeftViewModel(
        IMediator mediator,
        INavigator navigator,
        ILogger<AppHeaderLeftViewModel> logger)
    {
        _mediator = mediator;
        _navigator = navigator;
        _logger = logger;

        _logger.LogInformation("[AppHeaderLeft] Initialisiert - PageType: {PageType}, Title: {Title}",
            CurrentPageType, CurrentTitle);
    }

    /// <summary>
    /// Handles the PageNavigatedEvent - updates page type and title
    /// </summary>
    public Task Handle(PageNavigatedEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        _logger.LogInformation("[AppHeaderLeft] PageNavigatedEvent - PageType: {PageType}, Title: {Title}",
            @event.PageType, @event.Title);

        // Ensure property changes happen on UI thread
        var dispatcherQueue = App.MainWindow?.DispatcherQueue;
        if (dispatcherQueue != null)
        {
            dispatcherQueue.TryEnqueue(() =>
            {
                CurrentPageType = @event.PageType;
                CurrentTitle = string.IsNullOrWhiteSpace(@event.Title) ? "HEIMATPLATZ" : @event.Title;
                _logger.LogDebug("[AppHeaderLeft] Updated - PageType: {PageType}, Title: {Title}",
                    CurrentPageType, CurrentTitle);
            });
        }
        else
        {
            CurrentPageType = @event.PageType;
            CurrentTitle = string.IsNullOrWhiteSpace(@event.Title) ? "HEIMATPLATZ" : @event.Title;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Handles ToggleNavigationPaneEvent - forwarded from MainPage
    /// </summary>
    public Task Handle(ToggleNavigationPaneEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        // This event is handled by MainPage, but we receive it too
        // No action needed here
        return Task.CompletedTask;
    }

    /// <summary>
    /// Toggles the NavigationView pane (Hamburger menu) via Mediator event
    /// </summary>
    [RelayCommand]
    private async Task ToggleNavigationPane()
    {
        _logger.LogDebug("[AppHeaderLeft] ToggleNavigationPane");
        await _mediator.Publish(new ToggleNavigationPaneEvent());
    }

    /// <summary>
    /// Navigates back in the navigation stack
    /// </summary>
    [RelayCommand]
    private async Task GoBack()
    {
        _logger.LogDebug("[AppHeaderLeft] GoBack");
        await _navigator.NavigateBackAsync(this);
    }
}
