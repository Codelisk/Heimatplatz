using System.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// Was der Vollbild-Bildviewer von einem ViewModel braucht. Beide Detailseiten
/// (Immobilie und Zwangsversteigerung) teilen sich dadurch ein Overlay-Control
/// statt jeweils eigener, identischer XAML-Bloecke.
/// </summary>
public interface IImageViewerHost : INotifyPropertyChanged
{
    /// <summary>Volle Display-Variante des aktuellen Bildes</summary>
    string? CurrentFullImageUrl { get; }

    bool IsImageViewerOpen { get; set; }

    /// <summary>Blaetter-Pfeile: nur bei geoeffnetem Viewer UND mehreren Bildern</summary>
    bool ShowViewerNavigation { get; }

    /// <summary>Bild-Zaehler, z.B. "2 / 7"</summary>
    string ImageCounterText { get; }

    /// <summary>Barrierefreiheits-Beschreibung des Schliessen-Buttons</summary>
    string CloseViewerSemantic { get; }

    IRelayCommand ShowPreviousImageCommand { get; }
    IRelayCommand ShowNextImageCommand { get; }
    IRelayCommand CloseImageViewerCommand { get; }
}
