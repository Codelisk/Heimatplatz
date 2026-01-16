using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Seite zum Hinzufügen einer neuen Immobilie
/// </summary>
public sealed partial class AddPropertyPage : Page
{
    public AddPropertyViewModel ViewModel => (AddPropertyViewModel)DataContext;

    public AddPropertyPage()
    {
        InitializeComponent();

        // Beim Laden des ViewModels PropertyChanged-Listener hinzufügen
        Loaded += OnPageLoaded;
    }

    private void OnPageLoaded(object sender, RoutedEventArgs e)
    {
        if (ViewModel != null)
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }
    }

    private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Bei Fehler oder Success automatisch nach oben scrollen
        if (e.PropertyName == nameof(ViewModel.HasError) || e.PropertyName == nameof(ViewModel.ShowSuccess))
        {
            if (ViewModel.HasError || ViewModel.ShowSuccess)
            {
                PageScrollViewer.ChangeView(null, 0, null, false);
            }
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        // Navigation zurück zur HomePage
        if (Frame.CanGoBack)
        {
            Frame.GoBack();
        }
    }
}
