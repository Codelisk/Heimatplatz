namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// Attached Property fuer responsive Kachel-Listen: Die Spaltenanzahl
/// (GridItemsLayout.Span) einer CollectionView folgt aus verfuegbarer
/// Breite / MinItemWidth (Pendant zum UniformGridLayout.MinItemWidth der Uno-App).
/// Voraussetzung: ItemsLayout der CollectionView ist ein vertikales GridItemsLayout.
/// </summary>
public static class ResponsiveGrid
{
    public static readonly BindableProperty MinItemWidthProperty =
        BindableProperty.CreateAttached(
            "MinItemWidth",
            typeof(double),
            typeof(ResponsiveGrid),
            0d,
            propertyChanged: OnMinItemWidthChanged);

    public static double GetMinItemWidth(BindableObject view) => (double)view.GetValue(MinItemWidthProperty);

    public static void SetMinItemWidth(BindableObject view, double value) => view.SetValue(MinItemWidthProperty, value);

    private static void OnMinItemWidthChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not CollectionView collectionView)
            return;

        collectionView.SizeChanged -= OnSizeChanged;
        if (newValue is double width && width > 0)
            collectionView.SizeChanged += OnSizeChanged;
    }

    private static void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is not CollectionView collectionView || collectionView.Width <= 0)
            return;

        var minItemWidth = GetMinItemWidth(collectionView);
        if (minItemWidth <= 0 || collectionView.ItemsLayout is not GridItemsLayout gridLayout)
            return;

        var span = Math.Max(1, (int)(collectionView.Width / minItemWidth));
        if (gridLayout.Span != span)
            gridLayout.Span = span;
    }
}
