using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Anbieter-&amp;-Kontakt-Karte: read-only Anbieter-Zeile (kommt aus dem Profil,
/// wie im Web-Editor) plus optionaler zusaetzlicher Ansprechpartner. Der
/// Ansprechpartner landet serverseitig als zweiter Kontakt am Inserat
/// (DisplayOrder 1); leere Felder bedeuten "kein Ansprechpartner" und entfernen
/// ihn beim Speichern im Edit-Modus wieder.
/// </summary>
public partial class PropertyWizardViewModel
{
    /// <summary>Anbieter-Name aus dem Profil (Firmenname bei Makler/Verwaltung)</summary>
    [ObservableProperty]
    public partial string SellerPreviewName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsContactPersonHidden))]
    public partial bool IsContactPersonVisible { get; set; }

    /// <summary>Steuert die zugeklappte "+"-Zeile (kein Bool-Invert-Converter noetig)</summary>
    public bool IsContactPersonHidden => !IsContactPersonVisible;

    [ObservableProperty]
    public partial string ContactName { get; set; }

    [ObservableProperty]
    public partial string ContactEmail { get; set; }

    [ObservableProperty]
    public partial string ContactPhone { get; set; }

    [RelayCommand]
    private void AddContactPerson() => IsContactPersonVisible = true;

    [RelayCommand]
    private void RemoveContactPerson()
    {
        ContactName = string.Empty;
        ContactEmail = string.Empty;
        ContactPhone = string.Empty;
        IsContactPersonVisible = false;
    }

    private void InitializeContactStep()
    {
        SellerPreviewName = string.Empty;
        ContactName = string.Empty;
        ContactEmail = string.Empty;
        ContactPhone = string.Empty;
        IsContactPersonVisible = false;
    }

    /// <summary>
    /// Befuellt die Anbieter-Zeile: sofort aus der Session, danach praeziser aus dem
    /// Profil (Firmenname). Fehler sind unkritisch - die Zeile ist reine Vorschau.
    /// </summary>
    private async Task LoadSellerPreviewAsync()
    {
        SellerPreviewName = _authService.UserFullName ?? string.Empty;

        try
        {
            var (_, profile) = await _mediator.Request(new GetProfileHttpRequest());
            if (profile != null)
                SellerPreviewName = string.IsNullOrWhiteSpace(profile.CompanyName) ? profile.FullName : profile.CompanyName;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PropertyWizard] Anbieter-Vorschau konnte nicht geladen werden");
        }
    }

    /// <summary>Editor-Zustand -> Request-Objekt (null = kein Ansprechpartner).</summary>
    private ContactPersonInput? BuildContactPersonInput()
    {
        var name = NullIfEmpty(ContactName);
        var email = NullIfEmpty(ContactEmail);
        var phone = NullIfEmpty(ContactPhone);

        if (name == null && email == null && phone == null)
            return null;

        return new ContactPersonInput { Name = name ?? string.Empty, Email = email, Phone = phone };
    }

    /// <summary>Spiegelt PropertyFieldValidation.NormalizeContactPerson (Server bleibt massgeblich).</summary>
    private bool ValidateContactPerson()
    {
        var name = NullIfEmpty(ContactName);
        var email = NullIfEmpty(ContactEmail);
        var phone = NullIfEmpty(ContactPhone);

        if (name == null && email == null && phone == null)
            return true;

        if (name == null)
        {
            ErrorMessage = Loc.ValidationContactNameRequired;
            return false;
        }

        if (email == null && phone == null)
        {
            ErrorMessage = Loc.ValidationContactReachRequired;
            return false;
        }

        return true;
    }
}
