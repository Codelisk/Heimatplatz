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
/// ViewModel fuer die AddPropertyPage (neue Immobilie inserieren).
/// Formular mit typspezifischen Feldern (Haus/Grundstueck) und Bild-Upload via MediaPicker.
/// </summary>
[ShellMap<AddPropertyPage>("AddProperty")]
public partial class AddPropertyViewModel : ObservableObject, IPageLifecycleAware
{
    private const int MaxImages = 20;

    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILocationService _locationService;
    private readonly ILogger<AddPropertyViewModel> _logger;

    private List<LocationGemeindeDto> _municipalities = [];
    private bool _suppressSearch;

    // PropertyType-Auswahl fuer den Picker
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTyp))]
    [NotifyPropertyChangedFor(nameof(IsHouseType))]
    [NotifyPropertyChangedFor(nameof(IsLandType))]
    public partial PropertyTypeItem? SelectedPropertyTypeItem { get; set; }

    public PropertyType SelectedTyp => SelectedPropertyTypeItem?.Value ?? PropertyType.House;

    // Sichtbarkeit typspezifischer Felder
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
    public partial string SelectedOrtText { get; set; }

    public bool HasSelectedOrt => !string.IsNullOrEmpty(SelectedOrtText);

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

    public AddPropertyViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILocationService locationService,
        ILogger<AddPropertyViewModel> logger)
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
        // Inserieren erfordert ein angemeldetes Verkaeufer-Konto (API: RequireSeller) -
        // frueh abfangen statt spaeter mit rohem 401/403 zu scheitern
        if (!_authService.IsAuthenticated)
        {
            _ = _navigator.NavigateTo("Login", relativeNavigation: false);
            return;
        }

        if (!_authService.IsSeller)
        {
            ErrorMessage = "Inserate erstellen ist nur mit einem Verkäufer-Konto möglich.";
        }

        if (_municipalities.Count == 0)
        {
            _ = LoadMunicipalitiesAsync();
        }
    }

    public void OnDisappearing()
    {
    }

    #endregion

    private async Task LoadMunicipalitiesAsync()
    {
        try
        {
            _municipalities = await _locationService.GetAllMunicipalitiesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Orte");
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
    private async Task SavePropertyAsync()
    {
        ErrorMessage = null;

        if (!_authService.IsSeller)
        {
            ErrorMessage = "Inserate erstellen ist nur mit einem Verkäufer-Konto möglich.";
            return;
        }

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

        // Typfremde Felder nicht mitsenden (z.B. Wohnflaeche/Zimmer/Baujahr bei Grundstueck -
        // die Eingaben koennen von einem frueher gewaehlten Typ stammen und sind ausgeblendet)
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

            // Zuerst Bilder hochladen um URLs zu erhalten
            List<string>? imageUrls = null;
            try
            {
                var base64Images = Images.Select(img => new Base64ImageData
                {
                    FileName = img.FileName,
                    ContentType = img.ContentType,
                    Base64Data = img.ToBase64()
                }).ToList();

                _logger.LogInformation("[AddProperty] Starting image upload for {Count} files...", base64Images.Count);

                var (_, uploadResult) = await _mediator.Request(
                    new UploadPropertyImagesHttpRequest
                    {
                        Body = new UploadPropertyImagesRequest { Images = base64Images }
                    });

                imageUrls = uploadResult?.ImageUrls;
                _logger.LogInformation("[AddProperty] {Count} Bilder hochgeladen", imageUrls?.Count ?? 0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddProperty] FEHLER beim Hochladen der Bilder");
                ErrorMessage = "Fehler beim Hochladen der Bilder. Bitte versuchen Sie es erneut.";
                return;
            }

            _logger.LogInformation("[AddProperty] Creating property with MunicipalityId: {Id}, ImageUrls: {Count}",
                municipalityId, imageUrls?.Count ?? 0);

            await _mediator.Request(new CreatePropertyHttpRequest
            {
                Body = new CreatePropertyRequest
                {
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
            _logger.LogError(ex, "[AddProperty] Fehler beim Speichern");
            ErrorMessage = "Die Immobilie konnte nicht gespeichert werden. Bitte versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
        }

        if (saveSucceeded)
        {
            _logger.LogInformation("[AddProperty] Navigating to MyProperties after create");
            // Erst die gepushte(n) Erfassungsseite(n) vom Stack der Ursprungs-Section
            // nehmen, dann zur MyProperties-Root wechseln - sonst bliebe die
            // Erfassungsseite beim Rueckwechsel in die Ursprungs-Section sichtbar.
            await _navigator.PopToRoot();
            await _navigator.NavigateTo("MyProperties", relativeNavigation: false);
        }
    }

    /// <summary>
    /// Navigiert zurueck ohne zu speichern
    /// </summary>
    [RelayCommand]
    private Task Cancel() => _navigator.GoBack();

    /// <summary>
    /// True wenn der KI-Flow auf diesem Geraet der Primaerweg ist (Android/iOS-Phones)
    /// </summary>
    public bool ShowAiOption => PropertyCreationRoutes.IsAiDefault;

    /// <summary>
    /// Wechselt zur KI-gestuetzten Inseratserstellung
    /// </summary>
    [RelayCommand]
    private Task SwitchToAi() => _navigator.NavigateTo(PropertyCreationRoutes.Ai);

    #endregion
}
