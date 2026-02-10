using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using UploadRequest = Heimatplatz.Core.ApiClient.Generated.UploadPropertyImagesHttpRequest;
using UploadBase64 = Heimatplatz.Core.ApiClient.Generated.Base64ImageData;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using Windows.Storage.Pickers;
using PropertyType = Heimatplatz.Features.Properties.Contracts.Models.PropertyType;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for AddPropertyPage
/// Implements IPageInfo for header integration (shows back button)
/// Implements INavigationAware to trigger PageNavigatedEvent for header updates
/// </summary>
public partial class AddPropertyViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILocationService _locationService;
    private readonly ILogger<AddPropertyViewModel> _logger;

    // Edit Mode - Property ID if editing existing property
    [ObservableProperty]
    private Guid? _propertyId;

    public bool IsEditMode => PropertyId.HasValue;

    // PropertyType Items for ComboBox
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTyp), nameof(IsHouseType), nameof(IsLandType), nameof(IsForeclosureType))]
    private PropertyTypeItem? _selectedPropertyTypeItem;

    public Features.Properties.Contracts.Models.PropertyType SelectedTyp => SelectedPropertyTypeItem?.Value ?? Features.Properties.Contracts.Models.PropertyType.House;

    // Visibility properties based on selected property type
    public bool IsHouseType => SelectedTyp == Features.Properties.Contracts.Models.PropertyType.House;
    public bool IsLandType => SelectedTyp == Features.Properties.Contracts.Models.PropertyType.Land;
    public bool IsForeclosureType => SelectedTyp == Features.Properties.Contracts.Models.PropertyType.Foreclosure;

    // Common Fields
    [ObservableProperty]
    private string _titel = string.Empty;

    [ObservableProperty]
    private string _adresse = string.Empty;

    // Bezirke fuer den OrtPicker
    [ObservableProperty]
    private List<BezirkModel> _bezirke = [];

    // Ausgewaehlte Gemeinde (Single-Select OrtPicker)
    [ObservableProperty]
    private Guid? _selectedGemeindeId;

    [ObservableProperty]
    private string _preis = string.Empty;

    [ObservableProperty]
    private string _beschreibung = string.Empty;

    [ObservableProperty]
    private string _wohnflaecheM2 = string.Empty;

    [ObservableProperty]
    private string _grundstuecksflaecheM2 = string.Empty;

    [ObservableProperty]
    private string _zimmer = string.Empty;

    [ObservableProperty]
    private string _baujahr = string.Empty;

    // Foreclosure-Specific Fields
    [ObservableProperty]
    private Features.Properties.Contracts.Models.PropertyCategory _selectedPropertyCategory = Features.Properties.Contracts.Models.PropertyCategory.Einfamilienhaus;

    [ObservableProperty]
    private DateTimeOffset _auctionDate = DateTimeOffset.Now.AddDays(30);

    [ObservableProperty]
    private string _estimatedValue = string.Empty;

    [ObservableProperty]
    private string _minimumBid = string.Empty;

    [ObservableProperty]
    private string _caseNumber = string.Empty;

    [ObservableProperty]
    private string _court = string.Empty;

    [ObservableProperty]
    private string _status = "Aktiv";

    // Grundbuch-Daten
    [ObservableProperty]
    private string _registrationNumber = string.Empty;

    [ObservableProperty]
    private string _cadastralMunicipality = string.Empty;

    [ObservableProperty]
    private string _plotNumber = string.Empty;

    [ObservableProperty]
    private string _sheetNumber = string.Empty;

    // Flaechendaten (zusaetzliche)
    [ObservableProperty]
    private string _totalArea = string.Empty;

    [ObservableProperty]
    private string _buildingArea = string.Empty;

    [ObservableProperty]
    private string _gardenArea = string.Empty;

    // Immobilien-Details
    [ObservableProperty]
    private string _zoningDesignation = string.Empty;

    [ObservableProperty]
    private string _buildingCondition = string.Empty;

    // Versteigerungs-Details
    [ObservableProperty]
    private DateTimeOffset? _viewingDate;

    [ObservableProperty]
    private DateTimeOffset? _biddingDeadline;

    [ObservableProperty]
    private string _ownershipShare = string.Empty;

    [ObservableProperty]
    private string _edictUrl = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    // Dokumente (URLs)
    [ObservableProperty]
    private string _floorPlanUrl = string.Empty;

    [ObservableProperty]
    private string _sitePlanUrl = string.Empty;

    [ObservableProperty]
    private string _longAppraisalUrl = string.Empty;

    [ObservableProperty]
    private string _shortAppraisalUrl = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    private string? _errorMessage;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    [ObservableProperty]
    private bool _showSuccess;

    // Bilder
    public ObservableCollection<ImageItem> Images { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasImages), nameof(HasNoImages))]
    private int _imageCount;

    public bool HasImages => ImageCount > 0;
    public bool HasNoImages => ImageCount == 0;

    private const int MaxImages = 20;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Settings;
    public string PageTitle => "Immobilie hinzufügen";
    public Type? MainHeaderViewModel => null;

    #endregion

    public AddPropertyViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILocationService locationService,
        ILogger<AddPropertyViewModel> logger,
        AddPropertyData data)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _locationService = locationService;
        _logger = logger;

        // Set default property type
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"

        // Bezirke laden und ggf. Debug-Daten vorausfuellen
        _ = InitializeAsync(data);
    }

    private async Task InitializeAsync(AddPropertyData data)
    {
        await LoadBezirkeAsync();

        if (data.PrefillDebugData)
        {
            // Small delay to ensure Bezirke binding has propagated to OrtPicker
            // before we set SelectedGemeindeId
            await Task.Delay(100);
            PrefillWithDebugData();
        }
    }

    private async Task LoadBezirkeAsync()
    {
        try
        {
            var locations = await _locationService.GetLocationsAsync();
            Bezirke = locations
                .SelectMany(bl => bl.Bezirke)
                .Select(b => new BezirkModel(
                    b.Id,
                    b.Name,
                    b.Gemeinden.Select(g => new GemeindeModel(g.Id, g.Name, g.PostalCode)).ToList()
                ))
                .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler beim Laden der Orte");
        }
    }

    /// <summary>
    /// Called when page is navigated to with data (e.g., PropertyId for editing)
    /// </summary>
    public async Task OnNavigatedToAsync(IDictionary<string, object>? data)
    {
        if (data?.TryGetValue("PropertyId", out var propertyIdObj) == true && propertyIdObj is Guid id)
        {
            PropertyId = id;
            await LoadPropertyForEditingAsync(id);
        }
    }

    /// <summary>
    /// Fills the form with debug/test data for quick testing
    /// </summary>
    private void PrefillWithDebugData()
    {
        _logger.LogInformation("[AddProperty] Prefilling form with debug data");

        // Set property type to House
        SelectedPropertyTypeItem = PropertyTypes.FirstOrDefault(t => t.Value == PropertyType.House) ?? PropertyTypes[0];

        // Common fields
        Titel = "Schönes Einfamilienhaus in ruhiger Lage mit großem Garten";
        Adresse = "Musterstraße 42";
        Preis = "450000";
        Beschreibung = "Dieses wunderschöne Einfamilienhaus befindet sich in einer ruhigen Wohngegend und bietet alles, was das Herz begehrt. " +
                       "Ein großer Garten lädt zum Entspannen ein, während die moderne Ausstattung höchsten Wohnkomfort garantiert. " +
                       "Die Immobilie wurde 2015 komplett saniert und verfügt über eine effiziente Gasheizung.";

        // House-specific fields
        WohnflaecheM2 = "180";
        GrundstuecksflaecheM2 = "650";
        Zimmer = "5";
        Baujahr = "1985";

        // Select first available municipality if Bezirke are loaded
        if (Bezirke.Count > 0)
        {
            var firstGemeinde = Bezirke.SelectMany(b => b.Gemeinden).FirstOrDefault();
            if (firstGemeinde != null)
            {
                // Set both the model's IsSelected (for OrtPicker visual) and ViewModel's SelectedGemeindeId
                firstGemeinde.IsSelected = true;
                SelectedGemeindeId = firstGemeinde.Id;
            }
        }

        // DEBUG WORKAROUND: Auto-load test image
        _ = LoadDebugImageAsync();
    }

    /// <summary>
    /// DEBUG: Loads a test image automatically for quick testing.
    /// Must be called after a small delay to ensure UI thread context.
    /// </summary>
    private async Task LoadDebugImageAsync()
    {
        try
        {
            // Small delay to allow UI to be ready
            await Task.Delay(200);

            var imagePath = @"C:\Users\Daniel\Pictures\FRMkTEHXoAEjgIp.jpg";
            if (!System.IO.File.Exists(imagePath))
            {
                _logger.LogWarning("[AddProperty] Debug image not found: {Path}", imagePath);
                return;
            }

            _logger.LogInformation("[AddProperty] Loading debug image from: {Path}", imagePath);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(imagePath);

            // Create thumbnail from a temporary stream
            var thumbnail = new BitmapImage();
            using (var thumbnailStream = new MemoryStream(fileBytes))
            {
                await thumbnail.SetSourceAsync(thumbnailStream.AsRandomAccessStream());
            }

            // Store byte[] in ImageItem - streams are created fresh for each upload
            Images.Add(new ImageItem("FRMkTEHXoAEjgIp.jpg", "image/jpeg", fileBytes, thumbnail));
            ImageCount = Images.Count;

            _logger.LogInformation("[AddProperty] Debug image loaded successfully, ImageCount={Count}", ImageCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AddProperty] Failed to load debug image");
        }
    }

    /// <summary>
    /// Loads property data for editing
    /// </summary>
    private async Task LoadPropertyForEditingAsync(Guid propertyId)
    {
        IsBusy = true;
        ErrorMessage = null;

        try
        {
            // Load property details using GetPropertyByIdHttpRequest
            var result = await _mediator.Request(
                new GetPropertyByIdHttpRequest
                {
                    Id = propertyId
                }
            );

            if (result.Result?.Property != null)
            {
                var prop = result.Result.Property;

                // Fill common fields
                Titel = prop.Title;
                Adresse = prop.Address;
                SelectedGemeindeId = prop.MunicipalityId;
                Preis = prop.Price.ToString();
                Beschreibung = prop.Description ?? string.Empty;

                // Optional fields
                WohnflaecheM2 = prop.LivingAreaM2?.ToString() ?? string.Empty;
                GrundstuecksflaecheM2 = prop.PlotAreaM2?.ToString() ?? string.Empty;
                Zimmer = prop.Rooms?.ToString() ?? string.Empty;
                Baujahr = prop.YearBuilt?.ToString() ?? string.Empty;

                // Set property type
                var typeItem = PropertyTypes.FirstOrDefault(t => (int)t.Value == (int)prop.Type);
                if (typeItem != null)
                {
                    SelectedPropertyTypeItem = typeItem;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden der Immobilie: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }


    [RelayCommand]
    private async Task AddPhotosAsync()
    {
        try
        {
            if (Images.Count >= MaxImages)
            {
                ErrorMessage = $"Maximal {MaxImages} Bilder erlaubt";
                return;
            }

            var picker = new FileOpenPicker();
            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
            picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".webp");

            var files = await picker.PickMultipleFilesAsync();
            if (files == null || files.Count == 0)
                return;

            var remaining = MaxImages - Images.Count;
            var filesToAdd = files.Take(remaining);

            foreach (var file in filesToAdd)
            {
                using var fileStream = await file.OpenStreamForReadAsync();
                var contentType = file.ContentType;
                if (string.IsNullOrEmpty(contentType))
                    contentType = "image/jpeg";

                // Read entire file into byte array
                using var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);
                var fileBytes = memoryStream.ToArray();

                // Create thumbnail from the byte array
                var thumbnail = new BitmapImage();
                using (var thumbnailStream = new MemoryStream(fileBytes))
                {
                    await thumbnail.SetSourceAsync(thumbnailStream.AsRandomAccessStream());
                }

                // Store byte[] in ImageItem - streams are created fresh for each upload
                Images.Add(new ImageItem(file.Name, contentType, fileBytes, thumbnail));
            }

            ImageCount = Images.Count;

            if (files.Count > remaining)
            {
                ErrorMessage = $"Nur {remaining} weitere Bilder konnten hinzugefügt werden (max. {MaxImages})";
            }
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

    [RelayCommand]
    private async Task SavePropertyAsync()
    {
        _logger.LogInformation("[AddProperty] SavePropertyAsync CALLED - Images.Count={Count}", Images.Count);
        ErrorMessage = null;

        // Validation
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

        IsBusy = true;
        var isEdit = IsEditMode;
        var saveSucceeded = false;

        try
        {
            // Optional fields parsen
            int? wohnflaecheValue = null;
            if (!string.IsNullOrWhiteSpace(WohnflaecheM2) && int.TryParse(WohnflaecheM2, out var wf))
                wohnflaecheValue = wf;

            int? grundstuecksValue = null;
            if (!string.IsNullOrWhiteSpace(GrundstuecksflaecheM2) && int.TryParse(GrundstuecksflaecheM2, out var gs))
                grundstuecksValue = gs;

            int? zimmerValue = null;
            if (!string.IsNullOrWhiteSpace(Zimmer) && int.TryParse(Zimmer, out var z))
                zimmerValue = z;

            int? baujahrValue = null;
            if (!string.IsNullOrWhiteSpace(Baujahr) && int.TryParse(Baujahr, out var bj))
                baujahrValue = bj;

            // Get seller info from auth service
            var sellerName = _authService.UserFullName ?? "Unbekannt";

            // TypeSpecificData for future use
            Dictionary<string, object>? typeSpecificData = null;

            var municipalityId = SelectedGemeindeId!.Value;

            // Upload images first to get URLs
            List<string>? imageUrls = null;
            if (Images.Count > 0)
            {
                try
                {
                    // Convert images to Base64 for JSON upload
                    var base64Images = Images.Select(img => new UploadBase64
                    {
                        FileName = img.FileName,
                        ContentType = img.ContentType,
                        Base64Data = img.ToBase64()
                    }).ToList();

                    _logger.LogInformation("[AddProperty] Starting image upload for {Count} files...", base64Images.Count);

                    var (_, uploadResult) = await _mediator.Request(
                        new UploadRequest { Body = new Heimatplatz.Core.ApiClient.Generated.UploadPropertyImagesRequest { Images = base64Images } });

                    imageUrls = uploadResult.ImageUrls;
                    _logger.LogInformation("[AddProperty] {Count} Bilder hochgeladen: {Urls}",
                        imageUrls?.Count ?? 0,
                        string.Join(", ", imageUrls ?? []));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[AddProperty] FEHLER beim Hochladen der Bilder");
                    ErrorMessage = $"Fehler beim Hochladen der Bilder: {ex.Message}";
                    return; // Stop here, don't try to create property without images
                }
            }

            _logger.LogInformation("[AddProperty] Creating property with MunicipalityId: {Id}, ImageUrls: {Count}",
                municipalityId, imageUrls?.Count ?? 0);

            if (isEdit)
            {
                // Update existing property using generated Mediator request
                await _mediator.Request(new UpdatePropertyHttpRequest
                {
                    Body = new UpdatePropertyRequest
                    {
                        Id = PropertyId!.Value,
                        Title = Titel.Trim(),
                        Address = Adresse.Trim(),
                        MunicipalityId = municipalityId,
                        Price = (double)preisValue,
                        Type = (Core.ApiClient.Generated.PropertyType)(int)SelectedTyp,
                        SellerType = Core.ApiClient.Generated.SellerType.Private,
                        SellerName = sellerName,
                        Description = Beschreibung.Trim(),
                        LivingAreaSquareMeters = wohnflaecheValue,
                        PlotAreaSquareMeters = grundstuecksValue,
                        Rooms = zimmerValue,
                        YearBuilt = baujahrValue,
                        ImageUrls = imageUrls,
                        TypeSpecificData = typeSpecificData
                    }
                });
            }
            else
            {
                // Create new property with image URLs included
                var response = await _mediator.Request(new CreatePropertyHttpRequest
                {
                    Body = new CreatePropertyRequest
                    {
                        Title = Titel.Trim(),
                        Address = Adresse.Trim(),
                        MunicipalityId = municipalityId,
                        Price = (double)preisValue,
                        Type = (Core.ApiClient.Generated.PropertyType)(int)SelectedTyp,
                        SellerType = Core.ApiClient.Generated.SellerType.Private,
                        SellerName = sellerName,
                        Description = Beschreibung.Trim(),
                        LivingAreaSquareMeters = wohnflaecheValue,
                        PlotAreaSquareMeters = grundstuecksValue,
                        Rooms = zimmerValue,
                        YearBuilt = baujahrValue,
                        ImageUrls = imageUrls,
                        TypeSpecificData = typeSpecificData
                    }
                });
            }

            saveSucceeded = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }

        // Navigate AFTER IsBusy is false and try-finally has completed
        // Use direct navigation which works from any context (inside Main or at Shell level)
        if (saveSucceeded)
        {
            try
            {
                _logger.LogInformation("[AddProperty] Navigating to MyProperties after {Mode}", isEdit ? "edit" : "create");
                await _navigator.NavigateRouteAsync(this, "MyProperties");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AddProperty] Navigation nach dem Speichern fehlgeschlagen");
            }
        }
    }

    /// <summary>
    /// Navigiert zurück zur vorherigen Seite ohne zu speichern
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        _logger.LogInformation("[AddProperty] Cancel - navigating to MyProperties");
        await _navigator.NavigateRouteAsync(this, "MyProperties");
    }

    private void ResetForm()
    {
        Titel = string.Empty;
        Adresse = string.Empty;
        SelectedGemeindeId = null;
        Preis = string.Empty;
        Beschreibung = string.Empty;
        WohnflaecheM2 = string.Empty;
        GrundstuecksflaecheM2 = string.Empty;
        Zimmer = string.Empty;
        Baujahr = string.Empty;
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"

        // Reset foreclosure fields
        SelectedPropertyCategory = Features.Properties.Contracts.Models.PropertyCategory.Einfamilienhaus;
        AuctionDate = DateTimeOffset.Now.AddDays(30);
        EstimatedValue = string.Empty;
        MinimumBid = string.Empty;
        CaseNumber = string.Empty;
        Court = string.Empty;
        Status = "Aktiv";

        // Reset Grundbuch-Daten
        RegistrationNumber = string.Empty;
        CadastralMunicipality = string.Empty;
        PlotNumber = string.Empty;
        SheetNumber = string.Empty;

        // Reset Flaechendaten
        TotalArea = string.Empty;
        BuildingArea = string.Empty;
        GardenArea = string.Empty;

        // Reset Immobilien-Details
        ZoningDesignation = string.Empty;
        BuildingCondition = string.Empty;

        // Reset Versteigerungs-Details
        ViewingDate = null;
        BiddingDeadline = null;
        OwnershipShare = string.Empty;
        EdictUrl = string.Empty;
        Notes = string.Empty;

        // Reset Dokumente
        FloorPlanUrl = string.Empty;
        SitePlanUrl = string.Empty;
        LongAppraisalUrl = string.Empty;
        ShortAppraisalUrl = string.Empty;

        // Reset Bilder
        Images.Clear();
        ImageCount = 0;

        ShowSuccess = false;
    }

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter) { }

    /// <inheritdoc />
    public void OnNavigatedFrom() { }

    #endregion
}
