using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Presentation;

/// <summary>
/// ViewModel fuer das Impressum
/// </summary>
[ShellMap<ImprintPage>("Imprint")]
public partial class ImprintViewModel(IMediator mediator) : ObservableObject, IPageLifecycleAware
{
    [ObservableProperty]
    public partial bool IsLoading { get; set; }

    [ObservableProperty]
    public partial bool IsLoaded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    public partial string CompanyLine { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string OwnerLine { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string AddressLine { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ContactLine { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string UidLine { get; set; } = string.Empty;

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
            var (_, response) = await mediator.Request(new GetImprintHttpRequest());
            var imprint = response?.Imprint;
            if (imprint == null)
            {
                ErrorMessage = "Impressum konnte nicht geladen werden.";
                return;
            }

            CompanyLine = $"{imprint.CompanyName} {imprint.LegalForm}".Trim();
            OwnerLine = imprint.Owner ?? string.Empty;
            AddressLine = $"{imprint.Street}, {imprint.PostalCode} {imprint.City}, {imprint.Country}";
            ContactLine = $"E-Mail: {imprint.Email}  Tel: {imprint.Phone}";
            UidLine = string.IsNullOrEmpty(imprint.UidNumber) ? string.Empty : $"UID: {imprint.UidNumber}";
            VersionLine = $"Version {imprint.Version} - Stand: {imprint.LastUpdated:d}";

            Sections.Clear();
            if (imprint.Sections != null)
            {
                foreach (var section in imprint.Sections.Where(s => s.IsVisible).OrderBy(s => s.SortOrder))
                    Sections.Add(section);
            }

            IsLoaded = true;
        }
        catch (Exception)
        {
            ErrorMessage = "Impressum konnte nicht geladen werden.";
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
