using System.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// Page for displaying and managing user's blocked properties.
/// Blocked properties are hidden from the main property list.
/// Erbt von BasePage fuer automatisches INavigationAware Handling.
/// </summary>
public sealed partial class BlockedPage : BasePage
{
    public BlockedPage()
    {
        this.InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public BlockedViewModel? ViewModel => DataContext as BlockedViewModel;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is BlockedViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateVisibility(vm.IsEmpty);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(BlockedViewModel.IsEmpty) && sender is BlockedViewModel vm)
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
        ViewModel?.ViewPropertyDetailsCommand.Execute(property);
    }

    private void OnPropertyBlocked(object sender, PropertyListItemDto property)
    {
        // Auf der Blockiert-Seite bedeutet Klick = Entblockieren
        ViewModel?.RemoveFromCollectionCommand.Execute(property);
    }
}
