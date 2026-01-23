using Heimatplatz.Events;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;

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
