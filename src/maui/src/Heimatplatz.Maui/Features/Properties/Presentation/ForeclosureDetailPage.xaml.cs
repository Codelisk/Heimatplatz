using System.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class ForeclosureDetailPage : ContentPage
{
    private ForeclosureDetailViewModel? _viewModel;

    public ForeclosureDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = BindingContext as ForeclosureDetailViewModel;

        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForeclosureDetailViewModel.IsDescriptionExpanded)
            && _viewModel?.IsDescriptionExpanded == false)
            DescriptionScrollGuard.OnCollapsed(this, DetailScroll, DescriptionSection);
    }

    /// <summary>
    /// Zurueck schliesst zuerst die offene Lightbox statt die Seite zu verlassen
    /// (Android-Hardware-Back und Shell-Zurueck-Button).
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (BindingContext is ForeclosureDetailViewModel { IsImageViewerOpen: true } vm)
        {
            vm.IsImageViewerOpen = false;
            return true;
        }

        return base.OnBackButtonPressed();
    }

    /// <summary>
    /// Haelt auf Android die intern gewechselte Carousel-Position und den gebundenen
    /// Bildzaehler synchron.
    /// </summary>
    private void OnImagePositionChanged(object? sender, PositionChangedEventArgs e)
    {
        if (BindingContext is ForeclosureDetailViewModel vm && vm.CurrentImagePosition != e.CurrentPosition)
            vm.CurrentImagePosition = e.CurrentPosition;
    }
}
