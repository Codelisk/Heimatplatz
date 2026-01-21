using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// ViewModel fuer die Datenschutzerklaerung
/// </summary>
public partial class PrivacyPolicyViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private readonly IMediator _mediator;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private PrivacyPolicyDto? _privacyPolicy;

    public PrivacyPolicyViewModel(INavigator navigator, IMediator mediator)
    {
        _navigator = navigator;
        _mediator = mediator;
    }

    /// <summary>
    /// Name des Verantwortlichen
    /// </summary>
    public string ResponsibleName => PrivacyPolicy?.ResponsibleParty?.CompanyName ?? "Heimatplatz GmbH";

    /// <summary>
    /// Adresse des Verantwortlichen (formatiert)
    /// </summary>
    public string ResponsibleAddress
    {
        get
        {
            var party = PrivacyPolicy?.ResponsibleParty;
            if (party == null)
                return string.Empty;

            return $"{party.Street}, {party.PostalCode} {party.City}, {party.Country}";
        }
    }

    /// <summary>
    /// E-Mail des Verantwortlichen
    /// </summary>
    public string ResponsibleEmail => PrivacyPolicy?.ResponsibleParty?.Email ?? string.Empty;

    /// <summary>
    /// Telefon des Verantwortlichen (optional)
    /// </summary>
    public string? ResponsiblePhone => PrivacyPolicy?.ResponsibleParty?.Phone;

    /// <summary>
    /// Datenschutzbeauftragter (optional)
    /// </summary>
    public string? DataProtectionOfficer => PrivacyPolicy?.ResponsibleParty?.DataProtectionOfficer;

    /// <summary>
    /// Datum der letzten Aktualisierung (formatiert)
    /// </summary>
    public string LastUpdated
    {
        get
        {
            if (PrivacyPolicy == null)
                return string.Empty;

            return PrivacyPolicy.LastUpdated.ToString("dd. MMMM yyyy");
        }
    }

    /// <summary>
    /// Version der Datenschutzerklaerung
    /// </summary>
    public string Version => PrivacyPolicy?.Version ?? string.Empty;

    /// <summary>
    /// Gueltig ab Datum (formatiert)
    /// </summary>
    public string EffectiveDate
    {
        get
        {
            if (PrivacyPolicy == null)
                return string.Empty;

            return PrivacyPolicy.EffectiveDate.ToString("dd. MMMM yyyy");
        }
    }

    /// <summary>
    /// Sichtbare Abschnitte der Datenschutzerklaerung
    /// </summary>
    public IReadOnlyList<LegalSectionDto> Sections
        => PrivacyPolicy?.Sections?
            .Where(s => s.IsVisible)
            .OrderBy(s => s.SortOrder)
            .ToList()
        ?? [];

    /// <summary>
    /// Laedt die Datenschutzerklaerung vom Server
    /// </summary>
    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var response = await _mediator.Request(new GetPrivacyPolicyHttpRequest());

            if (response.Result?.PrivacyPolicy != null)
            {
                PrivacyPolicy = response.Result.PrivacyPolicy;
                NotifyPropertiesChanged();
            }
            else
            {
                ErrorMessage = "Datenschutzerklaerung konnte nicht geladen werden.";
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(ResponsibleName));
        OnPropertyChanged(nameof(ResponsibleAddress));
        OnPropertyChanged(nameof(ResponsibleEmail));
        OnPropertyChanged(nameof(ResponsiblePhone));
        OnPropertyChanged(nameof(DataProtectionOfficer));
        OnPropertyChanged(nameof(LastUpdated));
        OnPropertyChanged(nameof(Version));
        OnPropertyChanged(nameof(EffectiveDate));
        OnPropertyChanged(nameof(Sections));
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigator.GoBack(this);
    }
}
