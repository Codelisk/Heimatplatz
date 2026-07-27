using MapLibreNative.Maui.Handlers;

namespace Heimatplatz.Maui.Features.Debug.Presentation;

public partial class MapLibreSpikePage : ContentPage
{
    private bool _mapReady;
    private bool _styleApplied;

    public MapLibreSpikePage()
    {
        InitializeComponent();

        // Stil bewusst per Controller.SetStyleString statt StyleUrl-Property:
        // die Property hat im ersten Versuch still den Demotiles-Default geladen.
        SpikeMap.MapReadyCommand = new Command(() =>
        {
            _mapReady = true;
            if (Vm is not null) Vm.Status = "Karte bereit";
            TryApplyStyle();
        });
        SpikeMap.StyleLoadedCommand = new Command(() =>
        {
            if (Vm is not null)
                Vm.Status = _styleApplied
                    ? "Heimatplatz-Stil aktiv - Papier-Look + PMTiles pruefen"
                    : "Default-Stil geladen (unser Stil kam nicht an)";
        });
    }

    private MapLibreSpikeViewModel? Vm => BindingContext as MapLibreSpikeViewModel;

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Vm is not null) await Vm.LoadStyleAsync();
        TryApplyStyle();
    }

    private void TryApplyStyle()
    {
        if (_styleApplied || !_mapReady || Vm?.StyleJson is null) return;

        var controller = (SpikeMap.Handler as MapLibreMapHandler)?.Controller;
        if (controller is null) return;

        _styleApplied = true;
        controller.SetStyleString(Vm.StyleJson);
        // Zentrum der OOE-Bounds aus map-style.ts, Zoom passend fuer Landesueberblick
        controller.JumpTo(48.12, 13.87, 7.3);
        Vm.Status = "SetStyleString gesetzt - warte auf StyleLoaded ...";
    }
}
