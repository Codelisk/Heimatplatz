namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Holt beim Zusammenfalten der Beschreibung (Leporello-Falz) den
/// Abschnittsanfang wieder ins Bild: nach dem Schrumpfen des Inhalts wuerde
/// der Blick sonst irgendwo im nachfolgenden Seiteninhalt landen - gleicher
/// Guard wie im Web. Bewusst ohne Animation: ein sofortiger Sprung statt
/// Scroll-Tween (Performance vor Effekt).
/// </summary>
internal static class DescriptionScrollGuard
{
    public static void OnCollapsed(ContentPage page, ScrollView scroll, VisualElement section)
    {
        // Nach dem Layout-Pass ausfuehren, damit die Positionen bereits zur
        // geschrumpften Inhaltshoehe passen
        page.Dispatcher.Dispatch(async () =>
        {
            double y = 0;
            Element? element = section;
            while (element is VisualElement visual && !ReferenceEquals(element, scroll))
            {
                y += visual.Y;
                element = element.Parent;
            }

            var target = Math.Max(0, y - 12);
            if (scroll.ScrollY > target)
                await scroll.ScrollToAsync(0, target, animated: false);
        });
    }
}
