using CommunityToolkit.Mvvm.ComponentModel;

namespace Heimatplatz.Maui.Features.Properties.Models;

/// <summary>
/// Carousel-Item der Detailseiten: stabiles Objekt, dessen <see cref="Url"/> beim
/// Qualitaets-Upgrade (Thumbnail -&gt; Vorschau) in-place wechselt. Zwingend ein
/// Objekt mit Binding statt eines Strings in der Collection: jede
/// Collection-Notification (auch ein Replace) laesst den Android-CarouselView
/// SetCurrentItem + ScrollToPosition auf die aktuelle Position dispatchen und
/// bricht damit einen gerade laufenden Swipe ab - waehrend des sequenziellen
/// Preview-Nachladens war das Carousel dadurch sekundenlang nicht blaetterbar.
/// </summary>
public partial class DetailImage : ObservableObject
{
    public DetailImage(string url) => Url = url;

    [ObservableProperty]
    public partial string Url { get; set; }
}
