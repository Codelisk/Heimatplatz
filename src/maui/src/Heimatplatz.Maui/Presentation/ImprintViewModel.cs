using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Localization.Legal;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Presentation;

/// <summary>
/// ViewModel fuer das Impressum
/// </summary>
[ShellMap<ImprintPage>("Imprint")]
public partial class ImprintViewModel(
    IMediator mediator,
    ImprintStringsLocalized loc,
    ILogger<ImprintViewModel> logger) : ObservableObject, IPageLifecycleAware
{
    private string? _email;
    private string? _phoneLink;

    public ImprintStringsLocalized Loc => loc;

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
    public partial string EmailLine { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPhone))]
    public partial string PhoneLine { get; set; } = string.Empty;

    public bool HasPhone => !string.IsNullOrEmpty(PhoneLine);

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
                ErrorMessage = loc.LoadError;
                return;
            }

            CompanyLine = $"{imprint.CompanyName} {imprint.LegalForm}".Trim();
            OwnerLine = imprint.Owner ?? string.Empty;
            AddressLine = $"{imprint.Street}, {imprint.PostalCode} {imprint.City}, {imprint.Country}";
            _email = imprint.Email;
            // Server liefert die tel:-taugliche Form separat (PhoneLink) - der
            // Anzeigestring behaelt seine Leerzeichen/Schraegstriche.
            _phoneLink = imprint.PhoneLink;
            EmailLine = loc.EmailFormat(imprint.Email);
            PhoneLine = string.IsNullOrWhiteSpace(imprint.Phone) ? string.Empty : loc.PhoneFormat(imprint.Phone);
            UidLine = string.IsNullOrEmpty(imprint.UidNumber) ? string.Empty : loc.UidFormat(imprint.UidNumber);
            VersionLine = loc.VersionFormat(imprint.Version, imprint.LastUpdated);

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

    [RelayCommand]
    private async Task OpenEmailAsync()
    {
        if (string.IsNullOrWhiteSpace(_email))
            return;

        try
        {
            await Launcher.Default.OpenAsync(new Uri($"mailto:{_email}"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Imprint] Mail-App konnte nicht geoeffnet werden");
        }
    }

    [RelayCommand]
    private async Task OpenPhoneAsync()
    {
        if (string.IsNullOrWhiteSpace(_phoneLink))
            return;

        try
        {
            // PhoneLink ist die kompakte Nummer ("+43664..."), kein fertiger URI
            await Launcher.Default.OpenAsync(new Uri($"tel:{_phoneLink}"));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "[Imprint] Telefon-App konnte nicht geoeffnet werden");
        }
    }
}
