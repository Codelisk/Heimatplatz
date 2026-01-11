using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Core.ApiClient.Generated;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Generators;
using UnoFramework.ViewModels;

namespace Heimatplatz.Features.Immobilien.Presentation;

public partial class ImmobilienViewModel(BaseServices baseServices, IMediator mediator) : PageViewModel(baseServices), INavigationAware
{
    private readonly IMediator _mediator = mediator;

    [ObservableProperty]
    private string _title = "Immobilien";

    [ObservableProperty]
    private ObservableCollection<ImmobilieListeDto> _immobilien = [];

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private ImmobilienTyp? _selectedTyp;

    [ObservableProperty]
    private decimal _minPreis;

    [ObservableProperty]
    private decimal _maxPreis = 5_000_000m;

    [ObservableProperty]
    private string _ortSuche = string.Empty;

    [ObservableProperty]
    private decimal _minWohnflaeche;

    [ObservableProperty]
    private int _currentPage = 1;

    [ObservableProperty]
    private int _pageSize = 12;

    [UnoCommand]
    private async Task LoadAsync()
    {
        using (BeginBusy("Lade Immobilien..."))
        {
            try
            {
                // Load total count for header
                var countResponse = await _mediator.Request(new GetImmobilienAnzahlHttpRequest
                {
                    Typ = SelectedTyp,
                    Status = ImmobilienStatus.Aktiv
                });
                TotalCount = countResponse.Result.Anzahl;

                // Load properties
                var response = await _mediator.Request(new GetImmobilienHttpRequest
                {
                    Typ = SelectedTyp,
                    MinPreis = MinPreis > 0 ? (double)MinPreis : null,
                    MaxPreis = MaxPreis < 5_000_000m ? (double)MaxPreis : null,
                    MinWohnflaeche = MinWohnflaeche > 0 ? (double)MinWohnflaeche : null,
                    OrtSuche = string.IsNullOrWhiteSpace(OrtSuche) ? null : OrtSuche,
                    Seite = CurrentPage,
                    SeitenGroesse = PageSize,
                    Sortierung = ImmobilienSortierung.ErstelltAm,
                    Richtung = SortierRichtung.Absteigend
                });

                Immobilien = new ObservableCollection<ImmobilieListeDto>(response.Result.Eintraege);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fehler beim Laden der Immobilien");
            }
        }
    }

    [UnoCommand]
    private async Task ApplyFilterAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [UnoCommand]
    private async Task NavigateToDetailAsync(Guid id)
    {
        await Navigator.NavigateViewModelAsync<ImmobilieDetailViewModel>(this, data: new Dictionary<string, object>
        {
            ["Id"] = id
        });
    }

    public async void OnNavigatedTo(object? parameter)
    {
        await LoadCommand.ExecuteAsync(null);
    }

    public void OnNavigatedFrom()
    {
    }
}
