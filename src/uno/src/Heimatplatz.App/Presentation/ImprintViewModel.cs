using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using Shiny.Mediator;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// ViewModel fuer das Impressum
/// </summary>
public partial class ImprintViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IMediator _mediator;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Detail;
    public string PageTitle => "Impressum";
    public Type? MainHeaderViewModel => null;

    #endregion

    #region INavigationAware Implementation

    public void OnNavigatedTo(object? parameter)
    {
        if (Imprint is null)
        {
            _ = LoadAsync();
        }
    }

    public void OnNavigatedFrom()
    {
    }

    #endregion

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string? _errorMessage;

    [ObservableProperty]
    private ImprintDto? _imprint;

    public ImprintViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public string CompanyName => Imprint?.CompanyName ?? string.Empty;
    public string LegalForm => Imprint?.LegalForm ?? string.Empty;
    public string FullAddress
    {
        get
        {
            if (Imprint == null) return string.Empty;
            return $"{Imprint.Street}\n{Imprint.PostalCode} {Imprint.City}\n{Imprint.Country}";
        }
    }

    public string Email => Imprint?.Email ?? string.Empty;
    public string? Website => Imprint?.Website;
    public string UidNumber => Imprint?.UidNumber ?? string.Empty;
    public string? DunsNumber => Imprint?.DunsNumber;
    public string? Gln => Imprint?.Gln;
    public string? GisaNumber => Imprint?.GisaNumber;
    public string Trade => Imprint?.Trade ?? string.Empty;
    public string TradeAuthority => Imprint?.TradeAuthority ?? string.Empty;
    public string ProfessionalLaw => Imprint?.ProfessionalLaw ?? string.Empty;
    public string? ChamberMembership => Imprint?.ChamberMembership;
    public string? TradeGroup => Imprint?.TradeGroup;

    public string DisclaimerText => GetSectionContent(1);
    public string CopyrightText => GetSectionContent(2);
    public string DisputeText => GetSectionContent(3);

    public string LastUpdated
    {
        get
        {
            if (Imprint == null) return string.Empty;
            return Imprint.LastUpdated.ToString("dd. MMMM yyyy");
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var response = await _mediator.Request(new GetImprintHttpRequest());

            if (response.Result?.Imprint != null)
            {
                Imprint = response.Result.Imprint;
                NotifyPropertiesChanged();
            }
            else
            {
                ErrorMessage = "Impressum konnte nicht geladen werden.";
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

    private string GetSectionContent(int sortOrder)
    {
        var section = Imprint?.Sections?.FirstOrDefault(s => s.SortOrder == sortOrder);
        return section?.Content ?? string.Empty;
    }

    private void NotifyPropertiesChanged()
    {
        OnPropertyChanged(nameof(CompanyName));
        OnPropertyChanged(nameof(LegalForm));
        OnPropertyChanged(nameof(FullAddress));
        OnPropertyChanged(nameof(Email));
        OnPropertyChanged(nameof(Website));
        OnPropertyChanged(nameof(UidNumber));
        OnPropertyChanged(nameof(DunsNumber));
        OnPropertyChanged(nameof(Gln));
        OnPropertyChanged(nameof(GisaNumber));
        OnPropertyChanged(nameof(Trade));
        OnPropertyChanged(nameof(TradeAuthority));
        OnPropertyChanged(nameof(ProfessionalLaw));
        OnPropertyChanged(nameof(ChamberMembership));
        OnPropertyChanged(nameof(TradeGroup));
        OnPropertyChanged(nameof(DisclaimerText));
        OnPropertyChanged(nameof(CopyrightText));
        OnPropertyChanged(nameof(DisputeText));
        OnPropertyChanged(nameof(LastUpdated));
    }
}
