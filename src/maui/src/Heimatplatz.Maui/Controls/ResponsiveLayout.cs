namespace Heimatplatz.Maui.Controls;

/// <summary>
/// Drives the Compact, Medium and Expanded visual states from the actual
/// available width. Phones deliberately stay in Compact so their established
/// layout does not change when rotated.
/// </summary>
public static class ResponsiveLayout
{
    public const double DefaultMediumWidth = 900;
    public const double DefaultExpandedWidth = 1200;

    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ResponsiveLayout),
            false,
            propertyChanged: OnIsEnabledChanged);

    public static readonly BindableProperty MediumWidthProperty =
        BindableProperty.CreateAttached(
            "MediumWidth",
            typeof(double),
            typeof(ResponsiveLayout),
            DefaultMediumWidth,
            propertyChanged: OnBreakpointChanged);

    public static readonly BindableProperty ExpandedWidthProperty =
        BindableProperty.CreateAttached(
            "ExpandedWidth",
            typeof(double),
            typeof(ResponsiveLayout),
            DefaultExpandedWidth,
            propertyChanged: OnBreakpointChanged);

    public static readonly BindableProperty UseWindowWidthProperty =
        BindableProperty.CreateAttached(
            "UseWindowWidth",
            typeof(bool),
            typeof(ResponsiveLayout),
            false,
            propertyChanged: OnBreakpointChanged);

    public static bool GetIsEnabled(BindableObject view) => (bool)view.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject view, bool value) => view.SetValue(IsEnabledProperty, value);

    public static double GetMediumWidth(BindableObject view) => (double)view.GetValue(MediumWidthProperty);

    public static void SetMediumWidth(BindableObject view, double value) => view.SetValue(MediumWidthProperty, value);

    public static double GetExpandedWidth(BindableObject view) => (double)view.GetValue(ExpandedWidthProperty);

    public static void SetExpandedWidth(BindableObject view, double value) => view.SetValue(ExpandedWidthProperty, value);

    public static bool GetUseWindowWidth(BindableObject view) => (bool)view.GetValue(UseWindowWidthProperty);

    public static void SetUseWindowWidth(BindableObject view, bool value) => view.SetValue(UseWindowWidthProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not VisualElement element)
            return;

        element.SizeChanged -= OnSizeChanged;

        if (newValue is true)
        {
            element.SizeChanged += OnSizeChanged;
            ApplyState(element);
        }
    }

    private static void OnBreakpointChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is VisualElement element && GetIsEnabled(element))
            ApplyState(element);
    }

    private static void OnSizeChanged(object? sender, EventArgs e)
    {
        if (sender is VisualElement element)
            ApplyState(element);
    }

    private static void ApplyState(VisualElement element)
    {
        var mediumWidth = Math.Max(0, GetMediumWidth(element));
        var expandedWidth = Math.Max(mediumWidth, GetExpandedWidth(element));
        var availableWidth = GetUseWindowWidth(element) && element.Window?.Width > 0
            ? element.Window.Width
            : element.Width;

        var state = DeviceInfo.Current.Idiom == DeviceIdiom.Phone || availableWidth < mediumWidth
            ? "Compact"
            : availableWidth < expandedWidth
                ? "Medium"
                : "Expanded";

        VisualStateManager.GoToState(element, state);
    }
}
