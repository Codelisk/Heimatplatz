using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
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
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;
using Windows.Storage.Pickers;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for EditPropertyPage
/// </summary>
public partial class EditPropertyViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILocationService _locationService;
    private readonly ILogger<EditPropertyViewModel> _logger;

    // Property ID being edited
    [ObservableProperty]
    private Guid _propertyId;

    // PropertyType Items for ComboBox
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTyp), nameof(IsHouseType), nameof(IsLandType))]
    private PropertyTypeItem? _selectedPropertyTypeItem;

    public Features.Properties.Contracts.Models.PropertyType SelectedTyp => SelectedPropertyTypeItem?.Value ?? Features.Properties.Contracts.Models.PropertyType.House;

    // Visibility properties based on selected property type
    public bool IsHouseType => SelectedTyp == Features.Properties.Contracts.Models.PropertyType.House;
    public bool IsLandType => SelectedTyp == Features.Properties.Contracts.Models.PropertyType.Land;

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

    // UI State
    [ObservableProperty]
    private bool _isLoading;

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

    public EditPropertyViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILocationService locationService,
        ILogger<EditPropertyViewModel> logger,
        EditPropertyData data)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _locationService = locationService;
        _logger = logger;

        PropertyId = data.PropertyId;

        // Set default property type
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"

        // Load Bezirke and property data
        _ = InitializeAsync(data.PropertyId);
    }

    private async Task InitializeAsync(Guid propertyId)
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

        await LoadPropertyAsync(propertyId);
    }

    /// <summary>
    /// Loads property data for editing
    /// </summary>
    private async Task LoadPropertyAsync(Guid propertyId)
    {
        IsLoading = true;
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

                // Load existing images from server URLs
                if (prop.ImageUrls?.Count > 0)
                {
                    foreach (var url in prop.ImageUrls)
                    {
                        Images.Add(new ImageItem(
                            Path.GetFileName(new Uri(url).LocalPath),
                            "image/jpeg",
                            Array.Empty<byte>(),
                            Url: url));
                    }
                    ImageCount = Images.Count;
                }
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden der Immobilie: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task UpdatePropertyAsync()
    {
        ErrorMessage = null;
        ShowSuccess = false;

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

        IsLoading = true;

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

            var municipalityId = SelectedGemeindeId!.Value;

            // Collect existing image URLs and upload new images
            List<string>? imageUrls = null;
            if (Images.Count > 0)
            {
                try
                {
                    // Keep existing URLs as-is
                    var existingUrls = Images.Where(img => img.IsExisting).Select(img => img.Url!).ToList();

                    // Upload only new images
                    var newImages = Images.Where(img => !img.IsExisting).ToList();
                    List<string> newUrls = [];
                    if (newImages.Count > 0)
                    {
                        // Convert images to Base64 for JSON upload
                        var base64Images = newImages.Select(img => new UploadBase64
                        {
                            FileName = img.FileName,
                            ContentType = img.ContentType,
                            Base64Data = img.ToBase64()
                        }).ToList();

                        var (_, uploadResult) = await _mediator.Request(
                            new UploadRequest { Body = new Heimatplatz.Core.ApiClient.Generated.UploadPropertyImagesRequest { Images = base64Images } });

                        newUrls = uploadResult.ImageUrls ?? [];
                    }

                    imageUrls = [.. existingUrls, .. newUrls];
                    _logger.LogInformation("{Count} Bilder gesamt ({Existing} bestehend, {New} neu): {Urls}",
                        imageUrls.Count, existingUrls.Count, newUrls.Count,
                        string.Join(", ", imageUrls));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fehler beim Hochladen der Bilder");
                }
            }

            // Update property with image URLs included
            await _mediator.Request(new UpdatePropertyHttpRequest
            {
                Body = new UpdatePropertyRequest
                {
                    Id = PropertyId,
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
                    TypeSpecificData = null
                }
            });

            _logger.LogInformation("[EditProperty] Navigating to MyProperties after save");
            await _navigator.NavigateRouteAsync(this, "MyProperties");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
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

    #region IPageInfo Implementation

    public PageType PageType => PageType.Form;
    public string PageTitle => "Immobilie bearbeiten";

    #endregion

    #region INavigationAware Implementation

    public void OnNavigatedTo(object? parameter) { }
    public void OnNavigatedFrom() { }

    #endregion
}
