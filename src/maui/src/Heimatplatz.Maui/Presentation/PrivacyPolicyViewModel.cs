using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Localization.Legal;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Presentation;

/// <summary>
/// ViewModel fuer die Datenschutzerklaerung
/// </summary>
[ShellMap<PrivacyPolicyPage>("PrivacyPolicy")]
public partial class PrivacyPolicyViewModel(IMediator mediator, PrivacyPolicyStringsLocalized loc) : ObservableObject, IPageLifecycleAware
{
    public PrivacyPolicyStringsLocalized Loc => loc;

    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string ResponsibleParty { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string VersionLine { get; set; } = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<LegalSectionDto> Sections { get; } = [];

    public async void OnAppearing()
    {
        if (IsLoaded || IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;

        try
        {
            var (_, response) = await mediator.Request(new GetPrivacyPolicyHttpRequest());
            var policy = response?.PrivacyPolicy;
            if (policy == null)
            {
                ErrorMessage = loc.LoadError;
                return;
            }

            var party = policy.ResponsibleParty;
            ResponsibleParty = party == null
                ? string.Empty
                : loc.ResponsiblePartyFormat(party.CompanyName, party.Street, party.PostalCode, party.City, party.Country);
            VersionLine = loc.VersionFormat(policy.Version, policy.LastUpdated);

            Sections.Clear();
            if (policy.Sections != null)
            {
                foreach (var section in policy.Sections.Where(s => s.IsVisible).OrderBy(s => s.SortOrder))
                    Sections.Add(section);
            }

            IsLoaded = true;
        }
        catch (Exception)
        {
            ErrorMessage = loc.LoadError;
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void OnDisappearing()
    {
    }
}
