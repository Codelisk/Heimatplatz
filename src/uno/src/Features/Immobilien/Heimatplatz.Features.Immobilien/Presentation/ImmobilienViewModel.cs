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
        Console.WriteLine("=== LoadAsync started ===");
        Logger.LogInformation("=== LoadAsync started ===");

        using (BeginBusy("Lade Immobilien..."))
        {
            try
            {
                Logger.LogInformation("Requesting count from API...");

                // Load total count for header
                var countResponse = await _mediator.Request(new GetImmobilienAnzahlHttpRequest
                {
                    Typ = SelectedTyp,
                    Status = ImmobilienStatus.Aktiv
                });
                TotalCount = countResponse.Result.Anzahl;

                Logger.LogInformation("Count response received: {Count}", TotalCount);
                Logger.LogInformation("Requesting immobilien from API...");

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

                Logger.LogInformation("Immobilien response received: {Count} items", response.Result.Eintraege?.Count ?? 0);
                Immobilien = new ObservableCollection<ImmobilieListeDto>(response.Result.Eintraege ?? []);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fehler beim Laden der Immobilien: {Message}", ex.Message);
            }
        }

        Logger.LogInformation("=== LoadAsync finished ===");
    }

    [UnoCommand]
    private async Task ApplyFilterAsync()
    {
        CurrentPage = 1;
        await LoadAsync();
    }

    [UnoCommand]
    private async Task NavigateToDetailAsync(object? parameter)
    {
        Console.WriteLine($"=== NavigateToDetailAsync called with parameter: {parameter} (type: {parameter?.GetType().Name}) ===");
        Logger.LogInformation("NavigateToDetailAsync called with parameter: {Parameter} (type: {Type})", parameter, parameter?.GetType().Name);

        Guid? id = parameter switch
        {
            Guid guid => guid,
            ImmobilieListeDto dto => dto.Id,
            _ => null
        };

        Console.WriteLine($"=== Extracted ID: {id} ===");
        Logger.LogInformation("Extracted ID: {Id}", id);

        if (id.HasValue)
        {
            Console.WriteLine($"=== Navigating to detail page with ID: {id.Value} ===");
            Logger.LogInformation("Navigating to detail page with ID: {Id}", id.Value);
            try
            {
                var result = await Navigator.NavigateRouteAsync(this, "ImmobilieDetail", data: id.Value);
                Console.WriteLine($"=== Navigation result: {result} ===");
                Logger.LogInformation("Navigation result: {Result}", result);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Navigation FAILED with error: {ex.Message} ===");
                Logger.LogError(ex, "Navigation failed: {Message}", ex.Message);
            }
        }
        else
        {
            Console.WriteLine("=== ID is null, not navigating ===");
            Logger.LogWarning("NavigateToDetailAsync: ID is null, not navigating");
        }
    }

    public async void OnNavigatedTo(object? parameter)
    {
        Console.WriteLine("=== ImmobilienViewModel.OnNavigatedTo called ===");
        await LoadCommand.ExecuteAsync(null);
    }

    public void OnNavigatedFrom()
    {
    }
}
