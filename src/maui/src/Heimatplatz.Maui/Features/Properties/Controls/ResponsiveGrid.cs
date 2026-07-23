namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// Attached Properties fuer responsive Kachel-Listen: Die Spaltenanzahl
/// (GridItemsLayout.Span) einer CollectionView folgt aus verfuegbarer
/// Breite, sichtbarer Mindestbreite, Template-Padding und Item-Abstand.
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
            propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty ItemHorizontalPaddingProperty =
        BindableProperty.CreateAttached(
            "ItemHorizontalPadding",
            typeof(double),
            typeof(ResponsiveGrid),
            0d,
            propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty MaxColumnsProperty =
        BindableProperty.CreateAttached(
            "MaxColumns",
            typeof(int),
            typeof(ResponsiveGrid),
            int.MaxValue,
            propertyChanged: OnLayoutPropertyChanged);

    public static readonly BindableProperty HorizontalItemSpacingProperty =
        BindableProperty.CreateAttached(
            "HorizontalItemSpacing",
            typeof(double),
            typeof(ResponsiveGrid),
            0d,
            propertyChanged: OnLayoutPropertyChanged);

    public static double GetMinItemWidth(BindableObject view) => (double)view.GetValue(MinItemWidthProperty);

    public static void SetMinItemWidth(BindableObject view, double value) => view.SetValue(MinItemWidthProperty, value);

    public static double GetItemHorizontalPadding(BindableObject view) =>
        (double)view.GetValue(ItemHorizontalPaddingProperty);

    public static void SetItemHorizontalPadding(BindableObject view, double value) =>
        view.SetValue(ItemHorizontalPaddingProperty, value);

    public static int GetMaxColumns(BindableObject view) => (int)view.GetValue(MaxColumnsProperty);

    public static void SetMaxColumns(BindableObject view, int value) => view.SetValue(MaxColumnsProperty, value);

    public static double GetHorizontalItemSpacing(BindableObject view) =>
        (double)view.GetValue(HorizontalItemSpacingProperty);

    public static void SetHorizontalItemSpacing(BindableObject view, double value) =>
        view.SetValue(HorizontalItemSpacingProperty, value);

    private static void OnLayoutPropertyChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not CollectionView collectionView)
            return;

        collectionView.SizeChanged -= OnSizeChanged;
        if (GetMinItemWidth(collectionView) > 0)
        {
            collectionView.SizeChanged += OnSizeChanged;
            UpdateSpan(collectionView);
        }
    }

    private static void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is CollectionView collectionView)
            UpdateSpan(collectionView);
    }

    private static void UpdateSpan(CollectionView collectionView)
    {
        if (collectionView.Width <= 0)
            return;

        var minItemWidth = GetMinItemWidth(collectionView);
        if (minItemWidth <= 0 || collectionView.ItemsLayout is not GridItemsLayout gridLayout)
            return;

        var isPhone = DeviceInfo.Current.Idiom == DeviceIdiom.Phone;
        var templatePadding = Math.Max(0, GetItemHorizontalPadding(collectionView));
        var itemSpacing = isPhone ? 0 : Math.Max(0, GetHorizontalItemSpacing(collectionView));
        if (gridLayout.HorizontalItemSpacing != itemSpacing)
            gridLayout.HorizontalItemSpacing = itemSpacing;

        var requiredCellWidth = minItemWidth + templatePadding;
        var span = isPhone
            ? Math.Max(1, (int)(collectionView.Width / minItemWidth))
            : Math.Max(1, (int)Math.Floor(
                (collectionView.Width + itemSpacing) / (requiredCellWidth + itemSpacing)));

        if (!isPhone)
            span = Math.Min(span, Math.Max(1, GetMaxColumns(collectionView)));

        if (gridLayout.Span != span)
            gridLayout.Span = span;
    }
}
