using System.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

public partial class PropertyDetailPage : ContentPage
{
    private PropertyDetailViewModel? _viewModel;

    public PropertyDetailPage()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();

        if (_viewModel != null)
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

        _viewModel = BindingContext as PropertyDetailViewModel;

        if (_viewModel != null)
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    /// <summary>
    /// Zurueck schliesst zuerst die offene Lightbox statt die Seite zu verlassen
    /// (Android-Hardware-Back und Shell-Zurueck-Button).
    /// </summary>
    protected override bool OnBackButtonPressed()
    {
        if (_viewModel?.IsImageViewerOpen == true)
        {
            _viewModel.IsImageViewerOpen = false;
            return true;
        }

        return base.OnBackButtonPressed();
    }

    /// <summary>
    /// Android aktualisiert Carousel und Indicator intern, bevor das gebundene Position-
    /// Property zuverlaessig zurueckgeschrieben wird. Das Event haelt ViewModel, Zaehler
    /// und Lightbox-Navigation explizit auf derselben Position.
    /// </summary>
    private void OnImagePositionChanged(object? sender, PositionChangedEventArgs e)
    {
        if (_viewModel != null && _viewModel.CurrentImagePosition != e.CurrentPosition)
            _viewModel.CurrentImagePosition = e.CurrentPosition;
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PropertyDetailViewModel.IsContactExpanded))
            ForceFooterRelayout();

        if (e.PropertyName == nameof(PropertyDetailViewModel.IsDescriptionExpanded)
            && _viewModel?.IsDescriptionExpanded == false)
            DescriptionScrollGuard.OnCollapsed(this, DetailScroll, DescriptionSection);
    }

    /// <summary>
    /// Android zeichnet den End-verankerten Footer nach Groessenaenderung (Panel-Toggle)
    /// nicht zuverlaessig neu: der Visual Tree stimmt, aber der native Layout-Pass bleibt
    /// aus (stale Render). Expliziter RequestLayout/Invalidate auf dem nativen View heilt das.
    /// </summary>
    private void ForceFooterRelayout()
    {
        Dispatcher.Dispatch(() =>
        {
            ((IView)ContactFooter).InvalidateMeasure();
#if ANDROID
            if (ContactFooter.Handler?.PlatformView is Android.Views.View nativeView)
                InvalidateNativeTree(nativeView);
#endif
        });
    }

#if ANDROID
    /// <summary>
    /// Invalidiert rekursiv alle Views des Footers: Invalidate() auf dem Root reicht nicht,
    /// weil Kinder (z.B. das Chevron-Label) sonst mit altem Inhalt weitergezeichnet werden.
    /// </summary>
    private static void InvalidateNativeTree(Android.Views.View view)
    {
        view.RequestLayout();
        view.Invalidate();

        if (view is Android.Views.ViewGroup group)
        {
            for (var i = 0; i < group.ChildCount; i++)
            {
                var child = group.GetChildAt(i);
                if (child != null)
                    InvalidateNativeTree(child);
            }
        }
    }
#endif
}
