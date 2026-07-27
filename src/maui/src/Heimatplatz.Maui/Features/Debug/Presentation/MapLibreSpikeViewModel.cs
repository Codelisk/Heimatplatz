using CommunityToolkit.Mvvm.ComponentModel;
using Shiny;

namespace Heimatplatz.Maui.Features.Debug.Presentation;

/// <summary>
/// Wegwerf-Spike zur Evaluierung des MapLibreNative.Maui-Bindings: rendert der
/// native Renderer den Web-Kartenstil (Papier-Look) samt PMTiles-Vector-Tiles
/// von der Test-Origin? Bewusst ohne Localization und ohne Feature-Logik -
/// die Seite verschwindet nach der Entscheidung wieder (oder wird zur echten
/// nativen Kartenseite ausgebaut).
/// </summary>
[ShellMap<MapLibreSpikePage>("MapLibreSpike")]
public partial class MapLibreSpikeViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? StyleJson { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Lade Stil ...";

    public async Task LoadStyleAsync()
    {
        if (StyleJson is not null) return;

        var dark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var assetName = $"MapSpike/mapstyle-{(dark ? "dark" : "light")}.json";
        using var stream = await FileSystem.OpenAppPackageFileAsync(assetName);
        using var reader = new StreamReader(stream);
        StyleJson = await reader.ReadToEndAsync();
        Status = $"Stil geladen ({assetName}) - warte auf Karte ...";
    }
}
