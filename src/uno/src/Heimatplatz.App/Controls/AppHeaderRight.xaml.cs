using Heimatplatz.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Shiny.Mediator;
using UnoFramework.Contracts.Application;

namespace Heimatplatz.App.Controls;

/// <summary>
/// Right part of the AppHeader containing Auth buttons (Login/Register or User Profile).
/// Shares AppHeaderViewModel with AppHeaderLeft for consistent state.
/// DataContext (AppHeaderViewModel) is set automatically via Uno Navigation ViewMap.
/// </summary>
public sealed partial class AppHeaderRight : UserControl
{
    public AppHeaderRight()
    {
        this.InitializeComponent();
    }

    private async void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        // Navigate via Mediator event so MainPage handles it within the NavigationView content region.
        // Direct navigation from HeaderRight would navigate in the wrong region context.
        var mediator = ((IApplicationWithServices)Application.Current).Services!.GetRequiredService<IMediator>();
        await mediator.Publish(new NavigateToRouteInContentEvent("UserProfile"));
    }

    private async void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppHeaderRightViewModel viewModel)
        {
            await viewModel.LogoutAsync();
        }
    }
}
