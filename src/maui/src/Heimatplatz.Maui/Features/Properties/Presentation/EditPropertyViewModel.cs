using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die EditPropertyPage (eigene Immobilie bearbeiten).
/// Laedt die Immobilie anhand der PropertyId via API und speichert via UpdateProperty.
/// </summary>
[ShellMap<EditPropertyPage>("EditProperty")]
public partial class EditPropertyViewModel : ObservableObject, IPageLifecycleAware
{
    private const int MaxImages = 20;

    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILocationService _locationService;
    private readonly ILogger<EditPropertyViewModel> _logger;

    private List<LocationGemeindeDto> _municipalities = [];
    private bool _suppressSearch;
    private bool _isLoaded;

    /// <summary>
    /// Navigationsparameter: Id der zu bearbeitenden Immobilie
    /// </summary>
    [ShellProperty]
    public string PropertyId { get; set; } = string.Empty;

    // PropertyType-Auswahl fuer den Picker
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTyp))]
    [NotifyPropertyChangedFor(nameof(IsHouseType))]
    [NotifyPropertyChangedFor(nameof(IsLandType))]
    public partial PropertyTypeItem? SelectedPropertyTypeItem { get; set; }

    public PropertyType SelectedTyp => SelectedPropertyTypeItem?.Value ?? PropertyType.House;

    public bool IsHouseType => SelectedTyp == PropertyType.House;
    public bool IsLandType => SelectedTyp == PropertyType.Land;

    // Allgemeine Felder
    [ObservableProperty]
    public partial string Titel { get; set; }

    [ObservableProperty]
    public partial string Adresse { get; set; }

    [ObservableProperty]
    public partial string Preis { get; set; }

    [ObservableProperty]
    public partial string Beschreibung { get; set; }

    [ObservableProperty]
    public partial string WohnflaecheM2 { get; set; }

    [ObservableProperty]
    public partial string GrundstuecksflaecheM2 { get; set; }

    [ObservableProperty]
    public partial string Zimmer { get; set; }

    [ObservableProperty]
    public partial string Baujahr { get; set; }

    // Ort-Auswahl (Suche mit Vorschlaegen)
    [ObservableProperty]
    public partial string OrtSearchText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOrtSuggestions))]
    public partial List<LocationGemeindeDto> OrtSuggestions { get; set; }

    public bool HasOrtSuggestions => OrtSuggestions.Count > 0;

    [ObservableProperty]
    public partial Guid? SelectedGemeindeId { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedOrt))]
    [NotifyPropertyChangedFor(nameof(HasNoSelectedOrt))]
    public partial string SelectedOrtText { get; set; }

    public bool HasSelectedOrt => !string.IsNullOrEmpty(SelectedOrtText);

    /// <summary>Suchfeld nur zeigen solange kein Ort gewaehlt ist - es ist genau EIN Ort waehlbar.</summary>
    public bool HasNoSelectedOrt => !HasSelectedOrt;

    // UI-State
    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    // Bilder
    public ObservableCollection<ImageItem> Images { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImages))]
    [NotifyPropertyChangedFor(nameof(HasNoImages))]
    public partial int ImageCount { get; set; }

    public bool HasImages => ImageCount > 0;
    public bool HasNoImages => ImageCount == 0;

    public EditPropertyViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILocationService locationService,
        ILogger<EditPropertyViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _locationService = locationService;
        _logger = logger;

        Titel = string.Empty;
        Adresse = string.Empty;
        Preis = string.Empty;
        Beschreibung = string.Empty;
        WohnflaecheM2 = string.Empty;
        GrundstuecksflaecheM2 = string.Empty;
        Zimmer = string.Empty;
        Baujahr = string.Empty;
        OrtSearchText = string.Empty;
        OrtSuggestions = [];
        SelectedOrtText = string.Empty;
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        // Bearbeiten erfordert ein angemeldetes Verkaeufer-Konto (API: RequireSeller)
        if (!_authService.IsAuthenticated)
        {
            _ = _navigator.NavigateTo("Login", relativeNavigation: false);
            return;
        }

        if (_isLoaded) return;
        _isLoaded = true;

        _ = InitializeAsync();
    }

    public void OnDisappearing()
    {
    }

    #endregion

    private async Task InitializeAsync()
    {
        try
        {
            _municipalities = await _locationService.GetAllMunicipalitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Orte");
        }

        if (Guid.TryParse(PropertyId, out var id))
        {
            await LoadPropertyAsync(id);
        }
        else
        {
            _logger.LogWarning("[EditProperty] Invalid PropertyId: {PropertyId}", PropertyId);
        }
    }

    /// <summary>
    /// Laedt die Immobilie zur Bearbeitung
    /// </summary>
    private async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            var result = await _mediator.Request(new GetPropertyByIdHttpRequest { Id = propertyId });

            if (result.Result?.Property != null)
            {
                var prop = result.Result.Property;

                // Allgemeine Felder befuellen
                Titel = prop.Title;
                Adresse = prop.Address;
                SelectedGemeindeId = prop.MunicipalityId;
                Preis = prop.Price.ToString();
                Beschreibung = prop.Description ?? string.Empty;

                // Optionale Felder
                WohnflaecheM2 = prop.LivingAreaM2?.ToString() ?? string.Empty;
                GrundstuecksflaecheM2 = prop.PlotAreaM2?.ToString() ?? string.Empty;
                Zimmer = prop.Rooms?.ToString() ?? string.Empty;
                Baujahr = prop.YearBuilt?.ToString() ?? string.Empty;

                // Ort-Anzeige aus MunicipalityId aufloesen
                var gemeinde = _municipalities.FirstOrDefault(m => m.Id == prop.MunicipalityId);
                SelectedOrtText = gemeinde != null ? $"{gemeinde.Name} ({gemeinde.PostalCode})" : prop.City;

                // Immobilientyp setzen
                var typeItem = PropertyTypes.FirstOrDefault(t => t.Value == prop.Type);
                if (typeItem != null)
                {
                    SelectedPropertyTypeItem = typeItem;
                }

                // Bestehende Bilder von den Server-URLs laden
                Images.Clear();
                if (prop.ImageUrls?.Count > 0)
                {
                    foreach (var url in prop.ImageUrls)
                    {
                        // Relative URLs nicht mit new Uri() crashen lassen
                        var fileName = Uri.TryCreate(url, UriKind.Absolute, out var uri)
                            ? Path.GetFileName(uri.LocalPath)
                            : Path.GetFileName(url);

                        Images.Add(new ImageItem(
                            fileName,
                            "image/jpeg",
                            Array.Empty<byte>(),
                            Url: url));
                    }
                }
                ImageCount = Images.Count;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EditProperty] Fehler beim Laden der Immobilie {PropertyId}", PropertyId);
            ErrorMessage = "Die Immobilie konnte nicht geladen werden. Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    #region Ort-Suche

    partial void OnOrtSearchTextChanged(string value)
    {
        if (_suppressSearch) return;

        if (string.IsNullOrWhiteSpace(value) || value.Length < 2)
        {
            OrtSuggestions = [];
            return;
        }

        var search = value.Trim();
        OrtSuggestions = _municipalities
            .Where(m => m.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || m.PostalCode.StartsWith(search, StringComparison.OrdinalIgnoreCase))
            .Take(15)
            .ToList();
    }

    [RelayCommand]
    private void SelectGemeinde(LocationGemeindeDto gemeinde)
    {
        SelectedGemeindeId = gemeinde.Id;
        SelectedOrtText = $"{gemeinde.Name} ({gemeinde.PostalCode})";

        _suppressSearch = true;
        OrtSearchText = string.Empty;
        _suppressSearch = false;
        OrtSuggestions = [];
    }

    /// <summary>Hebt die Auswahl auf, damit ein anderer Ort gesucht werden kann.</summary>
    [RelayCommand]
    private void ClearOrtSelection()
    {
        SelectedGemeindeId = null;
        SelectedOrtText = string.Empty;

        _suppressSearch = true;
        OrtSearchText = string.Empty;
        _suppressSearch = false;
        OrtSuggestions = [];
    }

    #endregion

    #region Bilder

    /// <summary>
    /// Foto aus der Galerie hinzufuegen (MediaPicker)
    /// </summary>
    [RelayCommand]
    private async Task AddPhotoAsync()
    {
        try
        {
            if (Images.Count >= MaxImages)
            {
                ErrorMessage = $"Maximal {MaxImages} Bilder erlaubt";
                return;
            }

            var files = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Bilder auswählen"
            });
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (Images.Count >= MaxImages)
                {
                    ErrorMessage = $"Maximal {MaxImages} Bilder erlaubt";
                    break;
                }

                using var stream = await file.OpenReadAsync();
                using var memoryStream = new MemoryStream();
                await stream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                var contentType = string.IsNullOrEmpty(file.ContentType) ? "image/jpeg" : file.ContentType;

                Images.Add(new ImageItem(file.FileName, contentType, fileBytes));
            }

            ImageCount = Images.Count;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Auswählen von Bildern");
            ErrorMessage = $"Fehler beim Auswählen von Bildern: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveImage(ImageItem image)
    {
        Images.Remove(image);
        ImageCount = Images.Count;
    }

    #endregion

    #region Speichern

    /// <summary>
    /// Parst ein optionales Ganzzahl-Feld; ungueltige Eingaben werden nicht still
    /// verworfen, sondern erzeugen eine Fehlermeldung.
    /// </summary>
    private bool TryParseOptionalInt(string input, string fieldName, out int? value)
    {
        value = null;
        if (string.IsNullOrWhiteSpace(input))
            return true;

        if (int.TryParse(input.Trim(), out var parsed) && parsed >= 0)
        {
            value = parsed;
            return true;
        }

        ErrorMessage = $"Bitte geben Sie für {fieldName} eine gültige Zahl ein.";
        return false;
    }

    [RelayCommand]
    private async Task UpdatePropertyAsync()
    {
        ErrorMessage = null;

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

        if (string.IsNullOrWhiteSpace(Adresse))
        {
            ErrorMessage = "Bitte geben Sie eine Straße ein";
            return;
        }

        if (!SelectedGemeindeId.HasValue)
        {
            ErrorMessage = "Bitte wählen Sie einen Ort aus";
            return;
        }

        if (!Guid.TryParse(PropertyId, out var propertyId))
        {
            ErrorMessage = "Ungültige Immobilien-Id";
            return;
        }

        if (Images.Count == 0)
        {
            ErrorMessage = "Bitte fügen Sie mindestens ein Bild hinzu";
            return;
        }

        // Optionale Zahlenfelder validieren - ungueltige Eingaben nicht still verwerfen
        if (!TryParseOptionalInt(WohnflaecheM2, "Wohnfläche", out var wohnflaecheValue) ||
            !TryParseOptionalInt(GrundstuecksflaecheM2, "Grundstücksfläche", out var grundstuecksValue) ||
            !TryParseOptionalInt(Zimmer, "Zimmer", out var zimmerValue) ||
            !TryParseOptionalInt(Baujahr, "Baujahr", out var baujahrValue))
        {
            return;
        }

        // Typfremde Felder nicht mitsenden (Eingaben koennen von einem frueher gewaehlten Typ stammen)
        if (!IsHouseType)
        {
            wohnflaecheValue = null;
            zimmerValue = null;
            baujahrValue = null;
        }

        IsBusy = true;
        var saveSucceeded = false;

        try
        {
            var municipalityId = SelectedGemeindeId!.Value;

            // Bestehende URLs beibehalten, nur neue Bilder hochladen
            List<string> imageUrls;
            try
            {
                var existingUrls = Images.Where(img => img.IsExisting).Select(img => img.Url!).ToList();

                var newImages = Images.Where(img => !img.IsExisting).ToList();
                List<string> newUrls = [];
                if (newImages.Count > 0)
                {
                    var base64Images = newImages.Select(img => new Base64ImageData
                    {
                        FileName = img.FileName,
                        ContentType = img.ContentType,
                        Base64Data = img.ToBase64()
                    }).ToList();

                    var (_, uploadResult) = await _mediator.Request(
                        new UploadPropertyImagesHttpRequest
                        {
                            Body = new UploadPropertyImagesRequest { Images = base64Images }
                        });

                    newUrls = uploadResult?.ImageUrls ?? [];
                }

                imageUrls = [.. existingUrls, .. newUrls];
                _logger.LogInformation("{Count} Bilder gesamt ({Existing} bestehend, {New} neu)",
                    imageUrls.Count, existingUrls.Count, newUrls.Count);
            }
            catch (Exception ex)
            {
                // Update abbrechen - sonst wuerde ImageUrls unvollstaendig/leer gespeichert
                // und bestehende Bilder gingen still verloren
                _logger.LogError(ex, "Fehler beim Hochladen der Bilder");
                ErrorMessage = "Fehler beim Hochladen der Bilder. Die Änderungen wurden nicht gespeichert.";
                return;
            }

            await _mediator.Request(new UpdatePropertyHttpRequest
            {
                Body = new UpdatePropertyRequest
                {
                    Id = propertyId,
                    Title = Titel.Trim(),
                    Address = Adresse.Trim(),
                    MunicipalityId = municipalityId,
                    Price = (double)preisValue,
                    Type = SelectedTyp,
                    // SellerType/SellerName leitet der Server aus dem Benutzerprofil ab
                    Description = Beschreibung.Trim(),
                    LivingAreaSquareMeters = wohnflaecheValue,
                    PlotAreaSquareMeters = grundstuecksValue,
                    Rooms = zimmerValue,
                    YearBuilt = baujahrValue,
                    ImageUrls = imageUrls
                }
            });

            saveSucceeded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[EditProperty] Fehler beim Speichern");
            ErrorMessage = "Die Änderungen konnten nicht gespeichert werden. Bitte versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
        }

        if (saveSucceeded)
        {
            _logger.LogInformation("[EditProperty] Navigating back after save");
            await _navigator.GoBack();
        }
    }

    /// <summary>
    /// Navigiert zurueck ohne zu speichern
    /// </summary>
    [RelayCommand]
    private Task Cancel() => _navigator.GoBack();

    #endregion
}
