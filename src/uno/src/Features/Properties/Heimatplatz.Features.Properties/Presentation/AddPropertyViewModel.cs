using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Manual;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Shiny.Mediator;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for AddPropertyPage
/// </summary>
public partial class AddPropertyViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly UpdatePropertyManualClient _updatePropertyClient;
    private readonly INavigator _navigator;

    // Edit Mode - Property ID if editing existing property
    [ObservableProperty]
    private Guid? _propertyId;

    public bool IsEditMode => PropertyId.HasValue;

    // PropertyType Items for ComboBox
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTyp), nameof(IsHouseType), nameof(IsLandType), nameof(IsForeclosureType))]
    private PropertyTypeItem? _selectedPropertyTypeItem;

    public PropertyType SelectedTyp => SelectedPropertyTypeItem?.Value ?? PropertyType.House;

    // Visibility properties based on selected property type
    public bool IsHouseType => SelectedTyp == PropertyType.House;
    public bool IsLandType => SelectedTyp == PropertyType.Land;
    public bool IsForeclosureType => SelectedTyp == PropertyType.Foreclosure;

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
    private PropertyCategory _selectedPropertyCategory = PropertyCategory.Einfamilienhaus;

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

    public AddPropertyViewModel(
        IAuthService authService,
        IMediator mediator,
        UpdatePropertyManualClient updatePropertyClient,
        INavigator navigator)
    {
        _authService = authService;
        _mediator = mediator;
        _updatePropertyClient = updatePropertyClient;
        _navigator = navigator;

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
                new Heimatplatz.Core.ApiClient.Generated.GetPropertyByIdHttpRequest
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
    private async Task SavePropertyAsync()
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

        if (string.IsNullOrWhiteSpace(Adresse) || string.IsNullOrWhiteSpace(Ort) || string.IsNullOrWhiteSpace(Plz))
        {
            ErrorMessage = "Adresse, Ort und PLZ sind erforderlich";
            return;
        }

        IsBusy = true;

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
            var sellerType = 0; // Privat (default for all users)

            // TypeSpecificData for future use
            Dictionary<string, object>? typeSpecificData = null;

            if (IsEditMode)
            {
                // Update existing property using manual client
                // WORKAROUND: Using manual client due to Shiny Mediator OpenAPI generator bug
                // See: https://github.com/shinyorg/mediator/issues/54
                var updateRequest = new Heimatplatz.Core.ApiClient.Manual.UpdatePropertyRequestDto
                {
                    Title = Titel.Trim(),
                    Address = Adresse.Trim(),
                    City = Ort.Trim(),
                    PostalCode = Plz.Trim(),
                    Price = preisValue,
                    Type = (int)SelectedTyp,
                    SellerType = sellerType,
                    SellerName = sellerName,
                    Description = Beschreibung.Trim(),
                    LivingAreaSquareMeters = wohnflaecheValue,
                    PlotAreaSquareMeters = grundstuecksValue,
                    Rooms = zimmerValue,
                    YearBuilt = baujahrValue,
                    TypeSpecificData = typeSpecificData
                };

                await _updatePropertyClient.UpdatePropertyAsync(
                    PropertyId!.Value,
                    updateRequest);
            }
            else
            {
                // Create new property using generated client
                var response = await _mediator.Request(new Heimatplatz.Core.ApiClient.Generated.CreatePropertyHttpRequest
                {
                    Body = new Heimatplatz.Core.ApiClient.Generated.CreatePropertyRequest
                    {
                        Title = Titel.Trim(),
                        Address = Adresse.Trim(),
                        City = Ort.Trim(),
                        PostalCode = Plz.Trim(),
                        Price = (double)preisValue,
                        Type = (Heimatplatz.Core.ApiClient.Generated.PropertyType)SelectedTyp,
                        SellerType = (Heimatplatz.Core.ApiClient.Generated.SellerType)sellerType,
                        SellerName = sellerName,
                        Description = Beschreibung.Trim(),
                        LivingAreaSquareMeters = wohnflaecheValue,
                        PlotAreaSquareMeters = grundstuecksValue,
                        Rooms = zimmerValue,
                        YearBuilt = baujahrValue,
                        TypeSpecificData = typeSpecificData
                    }
                });
            }

            ShowSuccess = true;

            // Formular zurücksetzen (nur bei Create)
            if (!IsEditMode)
            {
                await Task.Delay(1500);
                ResetForm();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Navigiert zurück zur vorherigen Seite ohne zu speichern
    /// </summary>
    [RelayCommand]
    private async Task CancelAsync()
    {
        await _navigator.NavigateBackAsync(this);
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
        SelectedPropertyCategory = PropertyCategory.Einfamilienhaus;
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

        ShowSuccess = false;
    }
}
