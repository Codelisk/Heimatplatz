using System.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// MyPropertiesPage - Page for managing user's own properties
/// Erbt von BasePage fuer automatisches INavigationAware Handling und PageNavigatedEvent
/// </summary>
public sealed partial class MyPropertiesPage : BasePage
{
    public MyPropertiesPage()
    {
        this.InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public MyPropertiesViewModel? ViewModel => DataContext as MyPropertiesViewModel;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is MyPropertiesViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateVisibility(vm.IsEmpty);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MyPropertiesViewModel.IsEmpty) && sender is MyPropertiesViewModel vm)
        {
            DispatcherQueue?.TryEnqueue(() => UpdateVisibility(vm.IsEmpty));
        }
    }

    private void UpdateVisibility(bool isEmpty)
    {
        EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
        ContentArea.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
    }

    private void OnPropertyCardClicked(object sender, PropertyListItemDto property)
    {
        // Card-Klick öffnet Bearbeiten
        ViewModel?.EditPropertyCommand.Execute(property);
    }

    private void OnPropertyDeleted(object sender, PropertyListItemDto property)
    {
        // X-Button löscht die Immobilie
        ViewModel?.DeletePropertyCommand.Execute(property);
    }
}
