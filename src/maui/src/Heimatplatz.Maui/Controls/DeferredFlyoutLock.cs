namespace Heimatplatz.Maui.Controls;

/// <summary>
/// Sperrt den Flyout einer gepushten Seite erst, wenn die Navigation abgeschlossen ist.
///
/// Direkt in XAML (<c>Shell.FlyoutBehavior="Disabled"</c>) wertet Shell die Aenderung
/// bereits aus, waehrend die abgehende Seite noch vollstaendig am Schirm steht: Die
/// Toolbar verliert dabei sofort ihr Navigations-Icon und setzt den Zurueck-Pfeil erst
/// einen Message-Loop-Durchlauf spaeter. Dazwischen liegt mindestens ein gezeichnetes
/// Bild ohne Icon - Titel bzw. TitleView der alten Seite rutschen darin um die
/// Icon-Breite nach links und wieder zurueck. Auf Android sind das je nach Geraet
/// 100-250 ms, in denen die Kopfzeile sichtbar springt, bevor der Seitenwechsel
/// ueberhaupt animiert.
///
/// Nach <c>NavigatedTo</c> faellt derselbe Wechsel nicht mehr auf: Die Toolbar zeigt
/// dann ohnehin den Zurueck-Pfeil, das Icon behaelt seine Breite, und die alte
/// TitleView ist weg. Der Flyout ist waehrend der Animation kurz noch entsperrt -
/// erreichbar ist er in dieser Zeitspanne praktisch nicht.
/// </summary>
public static class DeferredFlyoutLock
{
    public static readonly BindableProperty IsEnabledProperty =
        BindableProperty.CreateAttached(
            "IsEnabled",
            typeof(bool),
            typeof(DeferredFlyoutLock),
            false,
            propertyChanged: OnIsEnabledChanged);

    public static bool GetIsEnabled(BindableObject view) => (bool)view.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(BindableObject view, bool value) => view.SetValue(IsEnabledProperty, value);

    private static void OnIsEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is not Page page)
            return;

        page.NavigatedTo -= OnNavigatedTo;

        if (newValue is true)
            page.NavigatedTo += OnNavigatedTo;
    }

    private static void OnNavigatedTo(object? sender, NavigatedToEventArgs e)
    {
        if (sender is Page page)
            Shell.SetFlyoutBehavior(page, FlyoutBehavior.Disabled);
    }
}
