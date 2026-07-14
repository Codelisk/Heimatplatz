using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Auth;
using Heimatplatz.Maui.Features.Properties.Models;
using PropertyCondition = Heimatplatz.Maui.Features.Properties.Models.PropertyCondition;
using Heimatplatz.Maui.Features.Properties.Services;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die PropertyDetailPage.
/// Laedt die Immobilie anhand der PropertyId via API (GetPropertyByIdHttpRequest).
/// </summary>
[ShellMap<PropertyDetailPage>("PropertyDetail")]
public partial class PropertyDetailViewModel : ObservableObject, IPageLifecycleAware
{
    private readonly IClipboardService _clipboardService;
    private readonly IShareService _shareService;
    private readonly IMediator _mediator;
    private readonly IAuthService _authService;
    private readonly IPropertyStatusService _propertyStatusService;
    private readonly ILogger<PropertyDetailViewModel> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Navigationsparameter: Id der zu ladenden Immobilie
    /// </summary>
    [ShellProperty]
    public string PropertyId { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string? BusyMessage { get; set; }

    [ObservableProperty]
    public partial PropertyDto? Property { get; set; }

    [ObservableProperty]
    public partial string Title { get; set; }

    [ObservableProperty]
    public partial string FormattedPrice { get; set; }

    [ObservableProperty]
    public partial string AddressText { get; set; }

    [ObservableProperty]
    public partial string ContactPersonText { get; set; }

    [ObservableProperty]
    public partial bool HasContactPerson { get; set; }

    [ObservableProperty]
    public partial string? CopyFeedback { get; set; }

    [ObservableProperty]
    public partial bool HasCopyFeedback { get; set; }

    [ObservableProperty]
    public partial bool IsAuthenticated { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonText))]
    [NotifyPropertyChangedFor(nameof(FavoriteButtonIcon))]
    public partial bool IsFavorite { get; set; }

    /// <summary>True wenn die Immobilie nicht geladen werden konnte (Fehler oder geloescht)</summary>
    [ObservableProperty]
    public partial bool HasLoadError { get; set; }

    [ObservableProperty]
    public partial string? LoadErrorText { get; set; }

    [ObservableProperty]
    public partial string TypeBadgeText { get; set; }

    [ObservableProperty]
    public partial Color TypeBadgeColor { get; set; }

    [ObservableProperty]
    public partial List<PropertyDetailSection> DetailSections { get; set; }

    [ObservableProperty]
    public partial string? Description { get; set; }

    [ObservableProperty]
    public partial bool HasDescription { get; set; }

    [ObservableProperty]
    public partial bool IsBroker { get; set; }

    [ObservableProperty]
    public partial string? OriginalListingUrl { get; set; }

    [ObservableProperty]
    public partial bool HasOriginalListingUrl { get; set; }

    [ObservableProperty]
    public partial bool HasImages { get; set; }

    [ObservableProperty]
    public partial List<string> ImageUrls { get; set; }

    [ObservableProperty]
    public partial string? SellerName { get; set; }

    [ObservableProperty]
    public partial List<ContactDisplayItem> Contacts { get; set; }

    [ObservableProperty]
    public partial bool HasContacts { get; set; }

    /// <summary>True wenn der Kontakt-Footer angezeigt wird (Anbieter oder Kontakte vorhanden)</summary>
    [ObservableProperty]
    public partial bool HasContactFooter { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ContactExpandIcon))]
    public partial bool IsContactExpanded { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ImageCounterText))]
    public partial int CurrentImagePosition { get; set; }

    /// <summary>
    /// Chevron: nach oben wenn zugeklappt (= aufklappen), nach unten wenn aufgeklappt (= zuklappen)
    /// </summary>
    public string ContactExpandIcon => IsContactExpanded ? "▼" : "▲";

    /// <summary>
    /// Bild-Zaehler fuer das Carousel, z.B. "2 / 7"
    /// </summary>
    public string ImageCounterText => ImageUrls.Count > 0
        ? $"{CurrentImagePosition + 1} / {ImageUrls.Count}"
        : string.Empty;

    /// <summary>
    /// Text fuer den Favoriten-Button je nach Status
    /// </summary>
    public string FavoriteButtonText => IsFavorite ? "Gemerkt" : "Merken";

    /// <summary>
    /// Icon fuer den Favoriten-Button (gefuelltes/leeres Herz)
    /// </summary>
    public string FavoriteButtonIcon => IsFavorite ? "♥" : "♡";

    public PropertyDetailViewModel(
        IClipboardService clipboardService,
        IShareService shareService,
        IMediator mediator,
        IAuthService authService,
        IPropertyStatusService propertyStatusService,
        ILogger<PropertyDetailViewModel> logger)
    {
        _clipboardService = clipboardService;
        _shareService = shareService;
        _mediator = mediator;
        _authService = authService;
        _propertyStatusService = propertyStatusService;
        _logger = logger;

        Title = "Immobilie";
        FormattedPrice = string.Empty;
        AddressText = string.Empty;
        ContactPersonText = string.Empty;
        TypeBadgeText = string.Empty;
        TypeBadgeColor = Colors.Gray;
        DetailSections = [];
        ImageUrls = [];
        Contacts = [];
        IsAuthenticated = authService.IsAuthenticated;
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        if (Guid.TryParse(PropertyId, out var id))
        {
            _ = LoadPropertyAsync(id);
        }
        else
        {
            _logger.LogWarning("[PropertyDetail] Invalid PropertyId: {PropertyId}", PropertyId);
        }
    }

    public void OnDisappearing()
    {
    }

    #endregion

    private async Task LoadPropertyAsync(Guid propertyId)
    {
        IsBusy = true;
        BusyMessage = "Lade Immobilie...";
        HasLoadError = false;
        LoadErrorText = null;

        try
        {
            _logger.LogInformation("[PropertyDetail] Loading property {PropertyId} from API", propertyId);

            var (_, response) = await _mediator.Request(new GetPropertyByIdHttpRequest { Id = propertyId });

            if (response?.Property != null)
            {
                Property = response.Property;
                _logger.LogInformation("[PropertyDetail] Property loaded: {Title}", Property.Title);
            }
            else
            {
                Property = null;
                _logger.LogWarning("[PropertyDetail] Property {PropertyId} not found", propertyId);
                HasLoadError = true;
                LoadErrorText = "Diese Immobilie ist nicht mehr verfügbar.";
            }

            // Favoriten-Status laden
            var isFavorite = false;
            if (_authService.IsAuthenticated)
            {
                await _propertyStatusService.EnsureLoadedAsync();
                isFavorite = _propertyStatusService.IsFavorite(propertyId);
            }

            UpdateDisplayProperties();
            IsFavorite = isFavorite;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PropertyDetail] Error loading property {PropertyId}", propertyId);
            Property = null;
            UpdateDisplayProperties();
            HasLoadError = true;
            LoadErrorText = "Die Immobilie konnte nicht geladen werden. Bitte überprüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private void UpdateDisplayProperties()
    {
        if (Property == null)
        {
            Title = "Immobilie";
            FormattedPrice = string.Empty;
            AddressText = string.Empty;
            ContactPersonText = string.Empty;
            HasContactPerson = false;
            IsBroker = false;
            OriginalListingUrl = null;
            HasOriginalListingUrl = false;
            HasImages = false;
            ImageUrls = [];
            SellerName = null;
            Contacts = [];
            HasContacts = false;
            HasContactFooter = false;
            IsContactExpanded = false;
            CurrentImagePosition = 0;
            OnPropertyChanged(nameof(ImageCounterText));
            TypeBadgeText = string.Empty;
            DetailSections = [];
            Description = null;
            HasDescription = false;
            return;
        }

        Title = Property.Title;

        // Typ-Badge Text
        TypeBadgeText = Property.Type switch
        {
            PropertyType.House => "HAUS",
            PropertyType.Land => "GRUND",
            PropertyType.Foreclosure => "ZV",
            _ => "IMM"
        };

        // Typ-Badge Farbe (analog zur PropertyCard)
        TypeBadgeColor = Property.Type switch
        {
            PropertyType.House => Color.FromArgb("#2D6A9F"),
            PropertyType.Land => Color.FromArgb("#5D8A66"),
            PropertyType.Foreclosure => Color.FromArgb("#B22222"),
            _ => Colors.Gray
        };

        // Preis formatieren: "3.590.000 €"
        FormattedPrice = PropertyDisplay.Price((decimal)Property.Price);

        // Volle Adresse: "Strasse, PLZ Ort"
        AddressText = $"{Property.Address}, {Property.PostalCode} {Property.City}";

        // Makler vs. Privat
        IsBroker = Property.SellerType == SellerType.Broker;
        OriginalListingUrl = Property.Contacts?.FirstOrDefault(c => !string.IsNullOrWhiteSpace(c.OriginalListingUrl))?.OriginalListingUrl;

        // Fuer Zwangsversteigerungen: EdictUrl aus TypeSpecificData extrahieren wenn keine Kontakt-URL
        if (string.IsNullOrWhiteSpace(OriginalListingUrl) && !string.IsNullOrWhiteSpace(Property.TypeSpecificData))
        {
            try
            {
                using var doc = JsonDocument.Parse(Property.TypeSpecificData);
                if (doc.RootElement.TryGetProperty("EdictUrl", out var edictUrlElement))
                    OriginalListingUrl = edictUrlElement.GetString();
            }
            catch
            {
                // Parse-Fehler ignorieren
            }
        }
        HasOriginalListingUrl = !string.IsNullOrWhiteSpace(OriginalListingUrl);

        // Bilder
        ImageUrls = Property.ImageUrls?.Where(url => !string.IsNullOrEmpty(url)).ToList() ?? [];
        HasImages = ImageUrls.Count > 0;
        CurrentImagePosition = 0;
        OnPropertyChanged(nameof(ImageCounterText));

        // Kontaktperson (erster Kontakt falls vorhanden); ausblenden wenn identisch
        // mit dem Anbieternamen (sonst steht derselbe Name doppelt im Footer)
        // Nur der Name: "Herr/Frau"-Praefix waere bei Firmenkontakten (GmbH, Makler) falsch
        var firstContact = Property.Contacts?.FirstOrDefault();
        ContactPersonText = firstContact?.Name ?? string.Empty;
        HasContactPerson = !string.IsNullOrWhiteSpace(ContactPersonText)
            && !string.Equals(ContactPersonText, Property.SellerName, StringComparison.OrdinalIgnoreCase);

        // Alle Kontakte fuer den aufklappbaren Kontaktbereich; Kontakte ohne
        // E-Mail/Telefon (nur Original-URL) deckt der Originalinserat-Button ab.
        // Kontaktname = Anbietername wird unterdrueckt (steht schon in der Kopfzeile).
        SellerName = Property.SellerName;
        Contacts = Property.Contacts?
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new ContactDisplayItem(
                string.Equals(c.Name, Property.SellerName, StringComparison.OrdinalIgnoreCase)
                    ? string.Empty
                    : c.Name ?? string.Empty,
                c.Email,
                c.Phone))
            .Where(c => c.HasEmail || c.HasPhone)
            .ToList() ?? [];
        HasContacts = Contacts.Count > 0;
        HasContactFooter = HasContacts || !string.IsNullOrWhiteSpace(SellerName);

        // Beschreibung
        Description = Property.Description;
        HasDescription = !string.IsNullOrWhiteSpace(Property.Description);

        // Strukturierte Datentabelle aufbauen
        BuildDetailSections();
    }

    private void BuildDetailSections()
    {
        if (Property == null) return;

        var items = new List<PropertyDetailItem>();
        var price = (decimal)Property.Price;

        // --- Basisdaten (hervorgehoben) ---
        var typeLabel = Property.Type switch
        {
            PropertyType.House => "Haus",
            PropertyType.Land => "Grundstück",
            PropertyType.Foreclosure => "Zwangsversteigerung",
            _ => Property.Type.ToString()
        };
        items.Add(new PropertyDetailItem("Immobilienart", typeLabel, PropertyDataCategory.Basisdaten, true));
        items.Add(new PropertyDetailItem("Kaufpreis", FormattedPrice, PropertyDataCategory.Basisdaten, true));
        AddIfNotEmpty(items, "PLZ", Property.PostalCode, PropertyDataCategory.Basisdaten, true);
        AddIfNotEmpty(items, "Ort", Property.City, PropertyDataCategory.Basisdaten, true);
        AddIfNotEmpty(items, "Adresse", Property.Address, PropertyDataCategory.Basisdaten, true);

        // TypeSpecificData deserialisieren
        HousePropertyData? houseData = null;
        LandPropertyData? landData = null;
        ForeclosurePropertyData? foreclosureData = null;

        if (!string.IsNullOrWhiteSpace(Property.TypeSpecificData))
        {
            try
            {
                switch (Property.Type)
                {
                    case PropertyType.House:
                        houseData = JsonSerializer.Deserialize<HousePropertyData>(Property.TypeSpecificData, JsonOptions);
                        break;
                    case PropertyType.Land:
                        landData = JsonSerializer.Deserialize<LandPropertyData>(Property.TypeSpecificData, JsonOptions);
                        break;
                    case PropertyType.Foreclosure:
                        foreclosureData = JsonSerializer.Deserialize<ForeclosurePropertyData>(Property.TypeSpecificData, JsonOptions);
                        break;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogWarning(ex, "[PropertyDetail] Failed to deserialize TypeSpecificData");
            }
        }

        // --- Flaechen ---
        if (houseData != null && houseData.LivingAreaInSquareMeters > 0)
            items.Add(new PropertyDetailItem("Wohnfläche", PropertyDisplay.Area(houseData.LivingAreaInSquareMeters), PropertyDataCategory.Flaechen));
        else
            AddIfHasValue(items, "Wohnfläche", Property.LivingAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);

        if (landData != null && landData.PlotSizeInSquareMeters > 0)
            items.Add(new PropertyDetailItem("Grundstücksfläche", PropertyDisplay.Area(landData.PlotSizeInSquareMeters), PropertyDataCategory.Flaechen));
        else
            AddIfHasValue(items, "Grundstücksfläche", Property.PlotAreaM2, v => PropertyDisplay.Area(v), PropertyDataCategory.Flaechen);

        // --- Gebaeude (Haus) ---
        if (houseData != null)
        {
            if (houseData.TotalRooms > 0)
                items.Add(new PropertyDetailItem("Zimmer", houseData.TotalRooms.ToString(), PropertyDataCategory.Gebaeude));
            else
                AddIfHasValue(items, "Zimmer", Property.Rooms, v => v.ToString(), PropertyDataCategory.Gebaeude);

            if (houseData.Bedrooms > 0)
                items.Add(new PropertyDetailItem("Schlafzimmer", houseData.Bedrooms.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.Bathrooms > 0)
                items.Add(new PropertyDetailItem("Badezimmer", houseData.Bathrooms.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.Floors > 0)
                items.Add(new PropertyDetailItem("Stockwerke", houseData.Floors.ToString(), PropertyDataCategory.Gebaeude));
            if (houseData.YearBuilt.HasValue)
                items.Add(new PropertyDetailItem("Baujahr", houseData.YearBuilt.Value.ToString(), PropertyDataCategory.Gebaeude));
            else
                AddIfHasValue(items, "Baujahr", Property.YearBuilt, v => v.ToString(), PropertyDataCategory.Gebaeude);

            // Default(0) ist kein definierter Enum-Wert (z.B. bei leerem TypeSpecificData "{}")
            if (Enum.IsDefined(houseData.Condition))
                items.Add(new PropertyDetailItem("Zustand", FormatCondition(houseData.Condition), PropertyDataCategory.Gebaeude));

            if (houseData.ApartmentFloor.HasValue)
                items.Add(new PropertyDetailItem("Etage", houseData.ApartmentFloor.Value.ToString(), PropertyDataCategory.Gebaeude));
        }
        else
        {
            // Fallback auf Kernfelder ohne HousePropertyData
            AddIfHasValue(items, "Zimmer", Property.Rooms, v => v.ToString(), PropertyDataCategory.Gebaeude);
            AddIfHasValue(items, "Baujahr", Property.YearBuilt, v => v.ToString(), PropertyDataCategory.Gebaeude);
        }

        // --- Ausstattung (Haus) ---
        if (houseData != null)
        {
            if (houseData.HasGarage)
                items.Add(new PropertyDetailItem("Garage", "Ja", PropertyDataCategory.Ausstattung));
            if (houseData.HasGarden)
                items.Add(new PropertyDetailItem("Garten", "Ja", PropertyDataCategory.Ausstattung));
            if (houseData.HasBasement)
                items.Add(new PropertyDetailItem("Keller", "Ja", PropertyDataCategory.Ausstattung));
            if (houseData.HasElevator == true)
                items.Add(new PropertyDetailItem("Aufzug", "Ja", PropertyDataCategory.Ausstattung));
        }

        // --- Grundstueck (Land) ---
        if (landData != null)
        {
            if (Enum.IsDefined(landData.Zoning))
                items.Add(new PropertyDetailItem("Widmung", FormatZoning(landData.Zoning), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem("Baurecht", FormatBool(landData.HasBuildingRights), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem("Bebaubar", FormatBool(landData.IsBuildable), PropertyDataCategory.Grundstueck));
            items.Add(new PropertyDetailItem("Versorgung", FormatBool(landData.HasUtilities), PropertyDataCategory.Grundstueck));
            if (landData.SoilQuality.HasValue)
                items.Add(new PropertyDetailItem("Bodenqualität", FormatSoilQuality(landData.SoilQuality.Value), PropertyDataCategory.Grundstueck));
        }

        // --- Versteigerung (Foreclosure) ---
        if (foreclosureData != null)
        {
            AddIfNotEmpty(items, "Gericht", foreclosureData.CourtName, PropertyDataCategory.Versteigerung);
            AddIfNotEmpty(items, "Aktenzeichen", foreclosureData.FileNumber, PropertyDataCategory.Versteigerung);
            if (foreclosureData.AuctionDate != default)
                items.Add(new PropertyDetailItem("Termin", foreclosureData.AuctionDate.ToString("dd.MM.yyyy"), PropertyDataCategory.Versteigerung));
            if (foreclosureData.MinimumBid > 0)
                items.Add(new PropertyDetailItem("Mindestgebot", PropertyDisplay.Price(foreclosureData.MinimumBid), PropertyDataCategory.Versteigerung));
            if (Enum.IsDefined(foreclosureData.Status))
                items.Add(new PropertyDetailItem("Status", PropertyDisplay.LegalStatusText(foreclosureData.Status), PropertyDataCategory.Versteigerung));
        }

        // --- Kosten ---
        if (Property.LivingAreaM2.HasValue && Property.LivingAreaM2.Value > 0)
        {
            var pricePerSqm = price / Property.LivingAreaM2.Value;
            items.Add(new PropertyDetailItem("Preis / m²", PropertyDisplay.PriceExact(pricePerSqm), PropertyDataCategory.Kosten));
        }

        // --- Basisdaten: Eingestellt am ---
        items.Add(new PropertyDetailItem("Eingestellt am", Property.CreatedAt.ToString("dd.MM.yyyy"), PropertyDataCategory.Basisdaten));

        // Nach Kategorie gruppieren, leere Sektionen entfallen automatisch
        DetailSections = items
            .GroupBy(i => i.Category)
            .OrderBy(g => g.Key)
            .Select(g => new PropertyDetailSection(GetCategoryTitle(g.Key), g.Key, g.ToList()))
            .ToList();
    }

    #region Formatting Helpers

    private static string FormatBool(bool value) => value ? "Ja" : "Nein";

    private static string FormatCondition(PropertyCondition condition) => condition switch
    {
        PropertyCondition.LikeNew => "Neuwertig",
        PropertyCondition.Good => "Gut",
        PropertyCondition.Average => "Durchschnittlich",
        PropertyCondition.NeedsRenovation => "Sanierungsbedürftig",
        _ => condition.ToString()
    };

    private static string FormatZoning(ZoningType zoning) => zoning switch
    {
        ZoningType.Residential => "Wohngebiet",
        ZoningType.Commercial => "Gewerbegebiet",
        ZoningType.Industrial => "Industriegebiet",
        ZoningType.Agricultural => "Landwirtschaft",
        ZoningType.Mixed => "Mischgebiet",
        _ => zoning.ToString()
    };

    private static string FormatSoilQuality(SoilQuality quality) => quality switch
    {
        SoilQuality.High => "Hoch",
        SoilQuality.Medium => "Mittel",
        SoilQuality.Low => "Niedrig",
        _ => quality.ToString()
    };

    private static string GetCategoryTitle(PropertyDataCategory category) => category switch
    {
        PropertyDataCategory.Basisdaten => "BASISDATEN",
        PropertyDataCategory.Flaechen => "FLÄCHEN",
        PropertyDataCategory.Gebaeude => "GEBÄUDE",
        PropertyDataCategory.Ausstattung => "AUSSTATTUNG",
        PropertyDataCategory.Grundstueck => "GRUNDSTÜCK",
        PropertyDataCategory.Versteigerung => "VERSTEIGERUNG",
        PropertyDataCategory.Rechtliches => "RECHTLICHES",
        PropertyDataCategory.Grundbuch => "GRUNDBUCH",
        PropertyDataCategory.Termine => "TERMINE",
        PropertyDataCategory.Kosten => "KOSTEN",
        PropertyDataCategory.Sonstiges => "SONSTIGES",
        _ => category.ToString().ToUpperInvariant()
    };

    private static void AddIfNotEmpty(List<PropertyDetailItem> items, string label, string? value, PropertyDataCategory category, bool highlighted = false)
    {
        if (!string.IsNullOrWhiteSpace(value))
            items.Add(new PropertyDetailItem(label, value, category, highlighted));
    }

    private static void AddIfHasValue<T>(List<PropertyDetailItem> items, string label, T? value, Func<T, string> format, PropertyDataCategory category) where T : struct
    {
        if (value.HasValue)
            items.Add(new PropertyDetailItem(label, format(value.Value), category));
    }

    #endregion

    #region Commands

    /// <summary>
    /// Kopiert einen Text in die Zwischenablage
    /// </summary>
    [RelayCommand]
    private async Task CopyToClipboardAsync(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return;

        var success = await _clipboardService.CopyToClipboardAsync(text);
        if (success)
        {
            await ShowCopyFeedbackAsync("Kopiert!", 1500);
        }
    }

    /// <summary>
    /// Wechselt den Favoriten-Status der aktuellen Immobilie
    /// </summary>
    [RelayCommand]
    private async Task ToggleFavoriteAsync()
    {
        if (Property == null || !_authService.IsAuthenticated)
            return;

        _logger.LogInformation("[PropertyDetail] Toggling favorite for {PropertyId}", Property.Id);

        IsFavorite = await _propertyStatusService.ToggleFavoriteAsync(Property.Id);
    }

    /// <summary>
    /// Teilt die Immobilie ueber nativen Share-Dialog oder Zwischenablage
    /// </summary>
    [RelayCommand]
    private async Task SharePropertyAsync()
    {
        if (Property == null)
            return;

        _logger.LogInformation("[PropertyDetail] Sharing property {PropertyId}", Property.Id);

        var propertyUrl = new Uri($"https://heimatplatz.at/immobilie/{Property.Id}");

        var description = $"{Property.Title}\n" +
                          $"{FormattedPrice}\n" +
                          $"{AddressText}";

        var result = await _shareService.ShareLinkAsync(Property.Title, propertyUrl, description);
        if (result == ShareResult.SharedNatively)
        {
            await ShowCopyFeedbackAsync("Geteilt!", 2000);
        }
        else if (result == ShareResult.CopiedToClipboard)
        {
            await ShowCopyFeedbackAsync("In Zwischenablage kopiert!", 2000);
        }
    }

    /// <summary>
    /// Klappt die Kontaktdetails im Footer auf/zu
    /// </summary>
    [RelayCommand]
    private void ToggleContactExpanded()
    {
        IsContactExpanded = !IsContactExpanded;
        _logger.LogDebug("[PropertyDetail] ToggleContactExpanded -> {State}", IsContactExpanded);
    }

    /// <summary>
    /// Oeffnet die Mail-App fuer eine Kontakt-E-Mail, Fallback: Adresse kopieren
    /// </summary>
    [RelayCommand]
    private async Task ContactByEmailAsync(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return;

        try
        {
            var subject = Uri.EscapeDataString($"Anfrage: {Property?.Title}");
            await Launcher.Default.OpenAsync(new Uri($"mailto:{email}?subject={subject}"));
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PropertyDetail] Mail-App konnte nicht geoeffnet werden");
        }

        await CopyToClipboardAsync(email);
    }

    /// <summary>
    /// Oeffnet die Telefon-App fuer eine Kontakt-Nummer, Fallback: Nummer kopieren
    /// </summary>
    [RelayCommand]
    private async Task CallPhoneAsync(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return;

        try
        {
            await Launcher.Default.OpenAsync(new Uri($"tel:{phone.Replace(" ", string.Empty)}"));
            return;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "[PropertyDetail] Telefon-App konnte nicht geoeffnet werden");
        }

        await CopyToClipboardAsync(phone);
    }

    /// <summary>
    /// Oeffnet die Original-Anbieterseite im Browser
    /// </summary>
    [RelayCommand]
    private async Task OpenOriginalListingAsync()
    {
        if (string.IsNullOrWhiteSpace(OriginalListingUrl))
            return;

        _logger.LogInformation("[PropertyDetail] Opening original listing: {Url}", OriginalListingUrl);

        await Launcher.Default.OpenAsync(new Uri(OriginalListingUrl));
    }

    private async Task ShowCopyFeedbackAsync(string message, int durationMs)
    {
        CopyFeedback = message;
        HasCopyFeedback = true;
        await Task.Delay(durationMs);
        CopyFeedback = null;
        HasCopyFeedback = false;
    }

    #endregion
}
