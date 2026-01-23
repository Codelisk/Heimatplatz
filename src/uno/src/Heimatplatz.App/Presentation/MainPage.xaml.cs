using Heimatplatz.Events;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// MainPage mit NavigationView - Titel wird von den einzelnen Pages via PageHeaderChangedEvent gesetzt.
/// Handled ToggleNavigationPaneEvent vom AppHeader fuer den Hamburger Button.
/// </summary>
public sealed partial class MainPage : Page, IEventHandler<ToggleNavigationPaneEvent>
{
    public MainPage()
    {
        this.InitializeComponent();

        // Initial zur AppHeader Region navigieren - DataContext wird automatisch via ViewMap gesetzt
        Loaded += async (_, _) =>
        {
            var navigator = this.Navigator();
            if (navigator != null)
            {
                await navigator.NavigateRouteAsync(this, "./AppHeader/AppHeader");
            }
        };
    }

    /// <summary>
    /// Handled das ToggleNavigationPaneEvent vom AppHeader (Hamburger Button)
    /// </summary>
    public new Task Handle(ToggleNavigationPaneEvent @event, IMediatorContext context, CancellationToken cancellationToken)
    {
        NavView.IsPaneOpen = !NavView.IsPaneOpen;
        return Task.CompletedTask;
    }
}
