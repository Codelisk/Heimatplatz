using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Uno.Extensions.Navigation;

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

    private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _ = this.Navigator()?.NavigateRouteAsync(this, "UserProfile");
    }

    private async void LogoutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is AppHeaderRightViewModel viewModel)
        {
            await viewModel.LogoutAsync();
        }
    }
}
