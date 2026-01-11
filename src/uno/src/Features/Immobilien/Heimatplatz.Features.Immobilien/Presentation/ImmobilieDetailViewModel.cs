using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Core.ApiClient.Generated;
using Heimatplatz.Features.Immobilien.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Generators;
using UnoFramework.ViewModels;

namespace Heimatplatz.Features.Immobilien.Presentation;

public partial class ImmobilieDetailViewModel(BaseServices baseServices, IMediator mediator) : PageViewModel(baseServices), INavigationAware
{
    private readonly IMediator _mediator = mediator;

    [ObservableProperty]
    private string _title = "Details";

    [ObservableProperty]
    private ImmobilieDetailDto? _immobilie;

    [ObservableProperty]
    private ObservableCollection<TechnicalFact> _technicalFacts = [];

    [ObservableProperty]
    private ObservableCollection<string> _highlights = [];

    [ObservableProperty]
    private int _totalCount;

    // Formatted properties for display
    public string PreisFormatiert => Immobilie is null ? string.Empty :
        Immobilie.Preis >= 1_000_000 ? $"\u20AC{Immobilie.Preis / 1_000_000:N2}M" : $"\u20AC{Immobilie.Preis:N0}";

    public string WohnflaecheFormatiert => Immobilie is null ? string.Empty : $"{Immobilie.Wohnflaeche:N0}m\u00B2";

    public string GrundstuecksflaecheFormatiert => Immobilie?.Grundstuecksflaeche is null ? string.Empty :
        $"{Immobilie.Grundstuecksflaeche:N0}m\u00B2";

    public string ZimmerFormatiert => Immobilie?.Zimmer?.ToString("N1") ?? string.Empty;

    public string HauptbildUrl => Immobilie?.Bilder?.FirstOrDefault(b => b.IstHauptbild)?.Url
        ?? Immobilie?.Bilder?.FirstOrDefault()?.Url
        ?? string.Empty;

    public string OrtUppercase => Immobilie?.Ort?.ToUpperInvariant() ?? string.Empty;

    [UnoCommand]
    private async Task LoadAsync(Guid id)
    {
        using (BeginBusy("Lade Details..."))
        {
            try
            {
                // Load total count for header
                var countResponse = await _mediator.Request(new GetImmobilienAnzahlHttpRequest
                {
                    Status = ImmobilienStatus.Aktiv
                });
                TotalCount = countResponse.Result.Anzahl;

                // Load property details
                var response = await _mediator.Request(new GetImmobilieByIdHttpRequest
                {
                    Id = id
                });

                Immobilie = response.Result.Immobilie;

                if (Immobilie is not null)
                {
                    Title = Immobilie.Ort.ToUpperInvariant();
                    BuildTechnicalFacts();
                    BuildHighlights();

                    // Notify formatted properties changed
                    OnPropertyChanged(nameof(PreisFormatiert));
                    OnPropertyChanged(nameof(WohnflaecheFormatiert));
                    OnPropertyChanged(nameof(GrundstuecksflaecheFormatiert));
                    OnPropertyChanged(nameof(ZimmerFormatiert));
                    OnPropertyChanged(nameof(HauptbildUrl));
                    OnPropertyChanged(nameof(OrtUppercase));
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Fehler beim Laden der Immobilie");
            }
        }
    }

    private void BuildTechnicalFacts()
    {
        if (Immobilie is null) return;

        var facts = new List<TechnicalFact>();

        if (Immobilie.Baujahr.HasValue)
            facts.Add(new TechnicalFact("CONSTRUCTION YEAR", Immobilie.Baujahr.Value.ToString()));

        // Note: These fields would need to be added to the API if needed
        // For now we show what's available
        facts.Add(new TechnicalFact("TYPE", Immobilie.Typ.ToString()));
        facts.Add(new TechnicalFact("STATUS", Immobilie.Status.ToString()));

        if (Immobilie.Schlafzimmer.HasValue)
            facts.Add(new TechnicalFact("BEDROOMS", Immobilie.Schlafzimmer.Value.ToString()));

        if (Immobilie.Badezimmer.HasValue)
            facts.Add(new TechnicalFact("BATHROOMS", Immobilie.Badezimmer.Value.ToString()));

        TechnicalFacts = new ObservableCollection<TechnicalFact>(facts);
    }

    private void BuildHighlights()
    {
        if (Immobilie is null) return;

        var highlights = new List<string>();

        if (Immobilie.Wohnflaeche > 200)
            highlights.Add("Grosszuegige Wohnflaeche");

        if (Immobilie.Grundstuecksflaeche > 1000)
            highlights.Add("Grosses Grundstueck");

        if (Immobilie.Zimmer > 5)
            highlights.Add("Viele Zimmer");

        if (Immobilie.Baujahr >= 2020)
            highlights.Add("Neubau/Erstbezug");

        if (!string.IsNullOrEmpty(Immobilie.Beschreibung))
        {
            if (Immobilie.Beschreibung.Contains("Pool", StringComparison.OrdinalIgnoreCase))
                highlights.Add("Pool");
            if (Immobilie.Beschreibung.Contains("Garage", StringComparison.OrdinalIgnoreCase))
                highlights.Add("Garage");
            if (Immobilie.Beschreibung.Contains("Garten", StringComparison.OrdinalIgnoreCase))
                highlights.Add("Garten");
            if (Immobilie.Beschreibung.Contains("Terrasse", StringComparison.OrdinalIgnoreCase))
                highlights.Add("Terrasse");
            if (Immobilie.Beschreibung.Contains("Seeblick", StringComparison.OrdinalIgnoreCase) ||
                Immobilie.Beschreibung.Contains("Seezugang", StringComparison.OrdinalIgnoreCase))
                highlights.Add("Seelage");
        }

        Highlights = new ObservableCollection<string>(highlights);
    }

    [UnoCommand]
    private async Task SendInquiryAsync()
    {
        // TODO: Implement inquiry form/dialog
        await Task.CompletedTask;
    }

    [UnoCommand]
    private async Task NavigateBackAsync()
    {
        await Navigator.NavigateBackAsync(this);
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is IDictionary<string, object> data && data.TryGetValue("Id", out var idObj) && idObj is Guid id)
        {
            await LoadCommand.ExecuteAsync(id);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
