using System.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;
using UnoFramework.Pages;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// FavoritesPage - Page for viewing favorited properties
/// Erbt von BasePage fuer automatisches INavigationAware Handling
/// </summary>
public sealed partial class FavoritesPage : BasePage
{
    public FavoritesPage()
    {
        this.InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    public FavoritesViewModel? ViewModel => DataContext as FavoritesViewModel;

    private void OnDataContextChanged(FrameworkElement sender, DataContextChangedEventArgs args)
    {
        if (args.NewValue is FavoritesViewModel vm)
        {
            vm.PropertyChanged += OnViewModelPropertyChanged;
            UpdateVisibility(vm.IsEmpty);
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(FavoritesViewModel.IsEmpty) && sender is FavoritesViewModel vm)
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

    private void OnPropertyFavorited(object sender, PropertyListItemDto property)
    {
        // Auf der Favoriten-Seite bedeutet Klick = Entfernen
        ViewModel?.RemoveFromCollectionCommand.Execute(property);
    }
}
