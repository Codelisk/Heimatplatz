using System.ComponentModel;
using Heimatplatz.Maui.Features.Properties.Controls;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class ForeclosureDetailPage : ContentPage
{
    private ForeclosureDetailViewModel? _viewModel;
    private PropertyImageViewerOverlay? _imageViewerOverlay;

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

    /// <summary>
    /// Zurueck schliesst zuerst die offene Lightbox statt die Seite zu verlassen
    /// (Android-Hardware-Back und Shell-Zurueck-Button).
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel is { IsImageViewerOpen: true })
        {
            _viewModel.IsImageViewerOpen = false;
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
        if (_viewModel != null && _viewModel.CurrentImagePosition != e.CurrentPosition)
            _viewModel.CurrentImagePosition = e.CurrentPosition;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ForeclosureDetailViewModel.IsImageViewerOpen) &&
            _viewModel?.IsImageViewerOpen == true)
        {
            EnsureImageViewerOverlay();
        }
    }

    /// <summary>
    /// Der Vollbild-Viewer entsteht erst, wenn ein Foto zum ersten Mal gross angesehen
    /// wird. Bei jeder Navigation mit aufzubauen kostet Zeit fuer etwas, das die meisten
    /// Aufrufe der Seite nie zu Gesicht bekommen.
    /// </summary>
    private void EnsureImageViewerOverlay()
    {
        if (_imageViewerOverlay != null)
            return;

        // Ueber beide Zeilen (Inhalt und Kontakt-Footer) wie der bisherige Inline-Viewer
        _imageViewerOverlay = new PropertyImageViewerOverlay("Foreclosure");
        Grid.SetRow(_imageViewerOverlay, 0);
        Grid.SetRowSpan(_imageViewerOverlay, 2);
        ForeclosureRoot.Add(_imageViewerOverlay);
    }
}
