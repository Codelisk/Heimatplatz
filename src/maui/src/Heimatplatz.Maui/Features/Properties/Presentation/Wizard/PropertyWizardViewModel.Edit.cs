using System.Globalization;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Edit-Modus: Eine bestehende (veroeffentlichte) Immobilie wird in derselben
/// WYSIWYG-Ansicht bearbeitet wie beim Erstellen - nur ohne Server-Entwuerfe und
/// ohne KI-Generierung. Gespeichert wird direkt via UpdateProperty; Fotos laufen
/// ueber dieselbe Upload-Pipeline wie im Erstell-Modus (bestehende bleiben als
/// Remote-URLs erhalten, neue werden beim Speichern hochgeladen).
/// </summary>
public partial class PropertyWizardViewModel
{
    /// <summary>Laedt die Immobilie frisch vom Server und befuellt den Editor.</summary>
    private async Task LoadPropertyForEditAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = Loc.BusyLoadingProperty;

        try
        {
            // Am LocalFirst-Cache vorbei: bearbeitet werden soll immer der aktuelle Stand
            var (_, response) = await _mediator.Request(
                new GetPropertyByIdHttpRequest { Id = propertyId },
                CancellationToken.None,
                static context => context.ForceCacheRefresh());

            if (response?.Property is not { } prop)
            {
                ErrorMessage = Loc.PropertyLoadError;
                return;
            }

            Media.Clear();
            foreach (var url in prop.ImageUrls ?? [])
                Media.Add(WizardMediaItem.FromRemote(url, isVideo: false));
            MediaCount = Media.Count;
            OnPropertyChanged(nameof(MediaCountText));

            SelectedPropertyTypeItem = PropertyTypes.FirstOrDefault(t => t.Value == prop.Type) ?? PropertyTypes[0];

            Titel = prop.Title;
            Zimmer = prop.Rooms?.ToString() ?? string.Empty;
            Wohnflaeche = prop.LivingAreaM2?.ToString() ?? string.Empty;
            Grundstuecksflaeche = prop.PlotAreaM2?.ToString() ?? string.Empty;
            Baujahr = prop.YearBuilt?.ToString() ?? string.Empty;
            SetFeatures(prop.Features ?? []);

            Adresse = prop.Address;
            Preis = ((decimal)prop.Price).ToString("0.##", CultureInfo.CurrentCulture);
            OriginalListingUrl = prop.Contacts
                ?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.OriginalListingUrl))
                ?.OriginalListingUrl ?? string.Empty;

            // Ansprechpartner = erster manuell gepflegter Kontakt jenseits des Anbieters;
            // MUSS vorbefuellt werden, sonst wuerde Speichern ihn als "entfernt" werten
            var contactPerson = prop.Contacts
                ?.Where(c => c.Source == ContactSource.Manual && c.DisplayOrder > 0)
                .OrderBy(c => c.DisplayOrder)
                .FirstOrDefault();
            ContactName = contactPerson?.Name ?? string.Empty;
            ContactEmail = contactPerson?.Email ?? string.Empty;
            ContactPhone = contactPerson?.Phone ?? string.Empty;
            IsContactPersonVisible = contactPerson != null;

            await Ort.RestoreByIdAsync(prop.MunicipalityId, prop.City);

            // Edit kennt nur den manuellen Beschreibungsweg (keine KI-Generierung)
            DescriptionMode = DraftDescriptionMode.Manual;
            Beschreibung = prop.Description ?? string.Empty;

            HeroIndex = 0;
            RefreshHero();
            RefreshEditorState();

            // Das Befuellen selbst zaehlt nicht als Aenderung
            _editDirty = false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Immobilie {PropertyId} konnte nicht geladen werden", propertyId);
            ErrorMessage = Loc.PropertyLoadErrorRetry;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Speichert die Aenderungen via UpdateProperty (wird vom Publish-Command im
    /// Edit-Modus aufgerufen - alle Validierungen sind dort bereits gelaufen).
    /// </summary>
    private async Task SaveChangesAsync()
    {
        if (!Guid.TryParse(EditPropertyId, out var propertyId))
        {
            ErrorMessage = Loc.InvalidPropertyId;
            return;
        }

        IsBusy = true;
        BusyMessage = Loc.BusySavingChanges;
        var saveSucceeded = false;

        try
        {
            // Neue Fotos jetzt hochladen; bestehende sind bereits Remote-URLs
            await EnsureMediaUploadedAsync();
            var imageUrls = UploadedImageUrls;
            if (imageUrls.Count == 0)
            {
                ErrorMessage = Loc.PhotosUploadFailed;
                return;
            }

            decimal.TryParse(Preis, out var preisValue);

            // Typfremde Felder nicht mitsenden (Eingaben koennen von einem frueher gewaehlten Typ stammen)
            var zimmer = IsHouseType ? ParseIntOrNull(Zimmer) : null;
            var wohnflaeche = IsHouseType ? ParseIntOrNull(Wohnflaeche) : null;
            var baujahr = IsHouseType ? ParseIntOrNull(Baujahr) : null;

            var (_, result) = await _mediator.Request(new UpdatePropertyHttpRequest
            {
                Body = new UpdatePropertyRequest
                {
                    Id = propertyId,
                    Title = Titel.Trim(),
                    Address = Adresse.Trim(),
                    MunicipalityId = Ort.SelectedGemeindeId!.Value,
                    Price = (double)preisValue,
                    Type = SelectedPropertyTypeItem?.Value ?? PropertyType.House,
                    // SellerType/SellerName leitet der Server aus dem Benutzerprofil ab
                    Description = Beschreibung.Trim(),
                    LivingAreaSquareMeters = wohnflaeche,
                    PlotAreaSquareMeters = ParseIntOrNull(Grundstuecksflaeche),
                    Rooms = zimmer,
                    YearBuilt = baujahr,
                    // Features immer mitsenden - null wuerde serverseitig alle Merkmale loeschen
                    Features = FeatureItems.ToList(),
                    ImageUrls = imageUrls,
                    OriginalListingUrl = string.IsNullOrWhiteSpace(OriginalListingUrl) ? null : OriginalListingUrl.Trim(),
                    ContactPerson = BuildContactPersonInput()
                }
            });

            if (result == null)
                throw new InvalidOperationException("Keine Antwort vom Server.");

            saveSucceeded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyWizard] Fehler beim Speichern der Änderungen");
            ErrorMessage = Loc.SaveChangesFailed;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }

        if (saveSucceeded)
        {
            _editDirty = false;

            // Kein Erfolgs-Alert: "Meine Immobilien" zeigt den frischen Stand sofort
            PropertyListRefreshSignal.Request();
            await _navigator.GoBack();
        }
    }
}
