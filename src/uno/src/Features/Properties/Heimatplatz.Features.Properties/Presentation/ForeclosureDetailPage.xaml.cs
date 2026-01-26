using Microsoft.UI.Xaml.Controls;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Detailseite fuer Zwangsversteigerungen
/// Zeigt alle relevanten Informationen zu einer Zwangsversteigerung
/// </summary>
public sealed partial class ForeclosureDetailPage : BasePage
{
    public ForeclosureDetailPage()
    {
        this.InitializeComponent();
    }

    public ForeclosureDetailViewModel? ViewModel => DataContext as ForeclosureDetailViewModel;

    private void OnImageSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (sender is FlipView flipView && ViewModel != null)
        {
            ViewModel.CurrentImageIndex = flipView.SelectedIndex + 1;
        }
    }
}
