using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Manual;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Shiny.Mediator;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel for EditPropertyPage
/// </summary>
public partial class EditPropertyViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly UpdatePropertyManualClient _updatePropertyClient;

    // Property ID being edited
    [ObservableProperty]
    private Guid _propertyId;

    // PropertyType Items for ComboBox
    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedTyp), nameof(IsHouseType), nameof(IsLandType))]
    private PropertyTypeItem? _selectedPropertyTypeItem;

    public PropertyType SelectedTyp => SelectedPropertyTypeItem?.Value ?? PropertyType.House;

    // Visibility properties based on selected property type
    public bool IsHouseType => SelectedTyp == PropertyType.House;
    public bool IsLandType => SelectedTyp == PropertyType.Land;

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
    private string _selectedAnbieterTyp = "Privat";

    [ObservableProperty]
    private string _anbieterName = string.Empty;

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

    public EditPropertyViewModel(
        IMediator mediator,
        UpdatePropertyManualClient updatePropertyClient)
    {
        _mediator = mediator;
        _updatePropertyClient = updatePropertyClient;

        // Set default property type
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
    }

    /// <summary>
    /// Called when page is navigated to with data (PropertyId)
    /// </summary>
    public async Task OnNavigatedToAsync(IDictionary<string, object>? data)
    {
        if (data?.TryGetValue("PropertyId", out var propertyIdObj) == true && propertyIdObj is Guid id)
        {
            PropertyId = id;
            await LoadPropertyAsync(id);
        }
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
                new Heimatplatz.Core.ApiClient.Generated.GetPropertyByIdHttpRequest
                {
                    Id = propertyId
                }
            );

            if (result.Result?.Property != null)
            {
                var prop = result.Result.Property;

                // Fill common fields
                Titel = prop.Titel;
                Adresse = prop.Adresse;
                Ort = prop.Ort;
                Plz = prop.Plz;
                Preis = prop.Preis.ToString();
                SelectedAnbieterTyp = prop.AnbieterTyp == Heimatplatz.Core.ApiClient.Generated.SellerType.Privat ? "Privat" : "Makler";
                AnbieterName = prop.AnbieterName;
                Beschreibung = prop.Beschreibung ?? string.Empty;

                // Optional fields
                WohnflaecheM2 = prop.WohnflaecheM2?.ToString() ?? string.Empty;
                GrundstuecksflaecheM2 = prop.GrundstuecksflaecheM2?.ToString() ?? string.Empty;
                Zimmer = prop.Zimmer?.ToString() ?? string.Empty;
                Baujahr = prop.Baujahr?.ToString() ?? string.Empty;

                // Set property type
                var typeItem = PropertyTypes.FirstOrDefault(t => (int)t.Value == (int)prop.Typ);
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

        if (string.IsNullOrWhiteSpace(Adresse) || string.IsNullOrWhiteSpace(Ort) || string.IsNullOrWhiteSpace(Plz))
        {
            ErrorMessage = "Adresse, Ort und PLZ sind erforderlich";
            return;
        }

        if (string.IsNullOrWhiteSpace(AnbieterName))
        {
            ErrorMessage = "Anbieter-Name ist erforderlich";
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

            // Map property type
            var propertyType = (int)SelectedTyp;

            // Map seller type
            var sellerType = SelectedAnbieterTyp == "Privat" ? 0 : 1;

            // Update property using manual client
            // WORKAROUND: Using manual client due to Shiny Mediator OpenAPI generator bug
            // See: https://github.com/shinyorg/mediator/issues/54
            var updateRequest = new UpdatePropertyRequestDto
            {
                Title = Titel.Trim(),
                Address = Adresse.Trim(),
                City = Ort.Trim(),
                PostalCode = Plz.Trim(),
                Price = preisValue,
                Type = propertyType,
                SellerType = sellerType,
                SellerName = AnbieterName.Trim(),
                Description = Beschreibung.Trim(),
                LivingAreaSquareMeters = wohnflaecheValue,
                PlotAreaSquareMeters = grundstuecksValue,
                Rooms = zimmerValue,
                YearBuilt = baujahrValue,
                TypeSpecificData = null
            };

            await _updatePropertyClient.UpdatePropertyAsync(PropertyId, updateRequest);

            ShowSuccess = true;

            // Wait a bit to show success message
            await Task.Delay(1500);
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
}
