using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml.Media.Imaging;
using Shiny.Mediator;
using Uno.Extensions.Navigation;
using Windows.Storage;
using Windows.Storage.Pickers;

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

    [ObservableProperty]
    private string _ort = string.Empty;

    [ObservableProperty]
    private string _plz = string.Empty;

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
        ILogger<AddPropertyViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _locationService = locationService;
        _logger = logger;

        // Set default property type
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
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
                Ort = prop.City;
                Plz = prop.PostalCode;
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

                // Read entire file into a MemoryStream (seekable, reusable for upload)
                var memoryStream = new MemoryStream();
                await fileStream.CopyToAsync(memoryStream);

                // Create thumbnail from the MemoryStream
                var thumbnail = new BitmapImage();
                memoryStream.Position = 0;
                await thumbnail.SetSourceAsync(memoryStream.AsRandomAccessStream());

                // Reset position for later upload use
                memoryStream.Position = 0;

                Images.Add(new ImageItem(file.Name, contentType, memoryStream, thumbnail));
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
        image.Stream.Dispose();
        Images.Remove(image);
        ImageCount = Images.Count;
    }

    [RelayCommand]
    private async Task SavePropertyAsync()
    {
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

        if (string.IsNullOrWhiteSpace(Adresse) || string.IsNullOrWhiteSpace(Ort) || string.IsNullOrWhiteSpace(Plz))
        {
            ErrorMessage = "Adresse, Ort und PLZ sind erforderlich";
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

            // Resolve MunicipalityId from Ort and Plz
            var municipalityId = await _locationService.ResolveMunicipalityIdAsync(Ort.Trim(), Plz.Trim());
            if (municipalityId == null)
            {
                // Fallback: Try to get first municipality
                var municipalities = await _locationService.GetAllMunicipalitiesAsync();
                municipalityId = municipalities.FirstOrDefault()?.Id;
                if (municipalityId == null)
                {
                    ErrorMessage = "Konnte keine passende Gemeinde für den angegebenen Ort finden";
                    return;
                }
            }

            // Upload images first to get URLs
            List<string>? imageUrls = null;
            if (Images.Count > 0)
            {
                try
                {
                    foreach (var img in Images)
                        img.Stream.Position = 0;

                    var imageFiles = Images.Select(img => new ImageFileData(
                        img.FileName,
                        img.ContentType,
                        img.Stream
                    )).ToList();

                    var (_, uploadResult) = await _mediator.Request(
                        new UploadPropertyImagesRequest(imageFiles));

                    imageUrls = uploadResult?.ImageUrls;
                    _logger.LogInformation("{Count} Bilder hochgeladen: {Urls}",
                        imageUrls?.Count ?? 0,
                        string.Join(", ", imageUrls ?? []));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Hochladen der Bilder");
                }
            }

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
                        MunicipalityId = municipalityId.Value,
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
                        MunicipalityId = municipalityId.Value,
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
        // Use NavigateToRouteInContentEvent so MainPage navigates via NavigationView,
        // which properly updates the NavigationView's selected item
        if (saveSucceeded)
        {
            try
            {
                _logger.LogInformation("Navigating to MyProperties after {Mode}", isEdit ? "edit" : "create");
                await _mediator.Publish(new Heimatplatz.Events.NavigateToRouteInContentEvent("MyProperties"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Navigation nach dem Speichern fehlgeschlagen");
            }
        }
    }

    /// <summary>
    /// Navigiert zurück zur vorherigen Seite ohne zu speichern
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await _mediator.Publish(new Heimatplatz.Events.NavigateToRouteInContentEvent("MyProperties"));
    }

    private void ResetForm()
    {
        Titel = string.Empty;
        Adresse = string.Empty;
        Ort = string.Empty;
        Plz = string.Empty;
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
        foreach (var img in Images)
            img.Stream.Dispose();
        Images.Clear();
        ImageCount = 0;

        ShowSuccess = false;
    }

    #region INavigationAware Implementation

    /// <inheritdoc />
    public void OnNavigatedTo(object? parameter)
    {
#if DEBUG
        // Auto-load test image since file picker can't be used via automation
        if (!IsEditMode)
            _ = LoadTestImageAsync();
#endif
    }

#if DEBUG
    private async Task LoadTestImageAsync()
    {
        try
        {
            var testImagePath = @"C:\Users\Daniel\Pictures\profilbild.jpg";
            if (!File.Exists(testImagePath))
                return;

            var fileBytes = await File.ReadAllBytesAsync(testImagePath);
            var memoryStream = new MemoryStream(fileBytes);

            var thumbnail = new BitmapImage();
            memoryStream.Position = 0;
            await thumbnail.SetSourceAsync(memoryStream.AsRandomAccessStream());

            memoryStream.Position = 0;
            Images.Add(new ImageItem("profilbild.jpg", "image/jpeg", memoryStream, thumbnail));
            ImageCount = Images.Count;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not load test image");
        }
    }
#endif

    /// <inheritdoc />
    public void OnNavigatedFrom()
    {
        // Cleanup if needed
    }

    #endregion
}
