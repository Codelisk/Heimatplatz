using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer AddPropertyPage
/// </summary>
public partial class AddPropertyViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly HttpClient _httpClient;

    [ObservableProperty]
    private string _titel = string.Empty;

    [ObservableProperty]
    private string _adresse = string.Empty;

    [ObservableProperty]
    private string _ort = string.Empty;

    [ObservableProperty]
    private string _plz = string.Empty;

    [ObservableProperty]
    private string _preis = string.Empty;

    [ObservableProperty]
    private PropertyType _selectedTyp = PropertyType.Haus;

    [ObservableProperty]
    private SellerType _selectedAnbieterTyp = SellerType.Privat;

    [ObservableProperty]
    private string _anbieterName = string.Empty;

    [ObservableProperty]
    private string _beschreibung = string.Empty;

    [ObservableProperty]
    private string _wohnflaecheM2 = string.Empty;

    [ObservableProperty]
    private string _grundstuecksflaecheM2 = string.Empty;

    [ObservableProperty]
    private string _zimmer = string.Empty;

    [ObservableProperty]
    private string _baujahr = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private bool _showSuccess;

    public AddPropertyViewModel(IAuthService authService, HttpClient httpClient)
    {
        _authService = authService;
        _httpClient = httpClient;

        // Verkäufer-Name mit aktuellem Benutzer vorausfüllen
        AnbieterName = _authService.UserFullName ?? string.Empty;
    }

    [RelayCommand]
    private async Task SavePropertyAsync()
    {
        ErrorMessage = null;
        ShowSuccess = false;

        // Validierung
        if (string.IsNullOrWhiteSpace(Titel) || Titel.Length < 10)
        {
            ErrorMessage = "Titel muss mindestens 10 Zeichen lang sein";
            return;
        }

        if (string.IsNullOrWhiteSpace(Beschreibung) || Beschreibung.Length < 50)
        {
            ErrorMessage = "Beschreibung muss mindestens 50 Zeichen lang sein";
            return;
        }

        if (!decimal.TryParse(Preis, out var preisValue) || preisValue <= 0)
        {
            ErrorMessage = "Bitte geben Sie einen gültigen Preis ein";
            return;
        }

        if (string.IsNullOrWhiteSpace(Adresse) || string.IsNullOrWhiteSpace(Ort) || string.IsNullOrWhiteSpace(Plz))
        {
            ErrorMessage = "Adresse, Ort und PLZ sind erforderlich";
            return;
        }

        if (string.IsNullOrWhiteSpace(AnbieterName))
        {
            ErrorMessage = "Anbieter-Name ist erforderlich";
            return;
        }

        IsBusy = true;

        try
        {
            // Optional fields parsen
            int? wohnflaecheValue = null;
            if (!string.IsNullOrWhiteSpace(WohnflaecheM2) && int.TryParse(WohnflaecheM2, out var wf))
                wohnflaecheValue = wf;

            int? grundstuecksValue = null;
            if (!string.IsNullOrWhiteSpace(GrundstuecksflaecheM2) && int.TryParse(GrundstuecksflaecheM2, out var gs))
                grundstuecksValue = gs;

            int? zimmerValue = null;
            if (!string.IsNullOrWhiteSpace(Zimmer) && int.TryParse(Zimmer, out var z))
                zimmerValue = z;

            int? baujahrValue = null;
            if (!string.IsNullOrWhiteSpace(Baujahr) && int.TryParse(Baujahr, out var bj))
                baujahrValue = bj;

            var dto = new CreatePropertyDto(
                Titel: Titel.Trim(),
                Adresse: Adresse.Trim(),
                Ort: Ort.Trim(),
                Plz: Plz.Trim(),
                Preis: preisValue,
                Typ: SelectedTyp,
                AnbieterTyp: SelectedAnbieterTyp,
                AnbieterName: AnbieterName.Trim(),
                Beschreibung: Beschreibung.Trim(),
                WohnflaecheM2: wohnflaecheValue,
                GrundstuecksflaecheM2: grundstuecksValue,
                Zimmer: zimmerValue,
                Baujahr: baujahrValue
            );

            var request = new HttpRequestMessage(HttpMethod.Post, "http://localhost:5292/api/properties")
            {
                Content = JsonContent.Create(dto)
            };

            // JWT Token hinzufügen
            if (!string.IsNullOrEmpty(_authService.AccessToken))
            {
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _authService.AccessToken);
            }

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                ShowSuccess = true;

                // Formular zurücksetzen
                await Task.Delay(1500);
                ResetForm();
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ErrorMessage = $"Fehler beim Speichern: {response.StatusCode}";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void ResetForm()
    {
        Titel = string.Empty;
        Adresse = string.Empty;
        Ort = string.Empty;
        Plz = string.Empty;
        Preis = string.Empty;
        Beschreibung = string.Empty;
        WohnflaecheM2 = string.Empty;
        GrundstuecksflaecheM2 = string.Empty;
        Zimmer = string.Empty;
        Baujahr = string.Empty;
        SelectedTyp = PropertyType.Haus;
        SelectedAnbieterTyp = SellerType.Privat;
        AnbieterName = _authService.UserFullName ?? string.Empty;
        ShowSuccess = false;
    }
}
