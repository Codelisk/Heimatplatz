using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Maui.ApiClient.Generated;
using Heimatplatz.Maui.Features.Properties.Models;
using Heimatplatz.Maui.Features.Properties.Services;

namespace Heimatplatz.Maui.Features.Properties.Presentation.Wizard;

/// <summary>
/// Schritt 4: Eckdaten pruefen. Das KI-Ergebnis wird ausschliesslich in noch leere
/// Felder uebernommen (Nutzereingaben gewinnen immer); laeuft die Analyse noch,
/// zeigt der Schritt einen Fortschritts-Banner ueber dem editierbaren Formular.
/// </summary>
public partial class PropertyWizardViewModel
{
    private bool _userChangedType;
    private bool _suppressTypeTracking;

    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsHouseType))]
    public partial PropertyTypeItem? SelectedPropertyTypeItem { get; set; }

    public bool IsHouseType => SelectedPropertyTypeItem?.Value == PropertyType.House;

    [ObservableProperty]
    public partial string Titel { get; set; }

    [ObservableProperty]
    public partial string Beschreibung { get; set; }

    [ObservableProperty]
    public partial string Zimmer { get; set; }

    [ObservableProperty]
    public partial string Wohnflaeche { get; set; }

    [ObservableProperty]
    public partial string Grundstuecksflaeche { get; set; }

    [ObservableProperty]
    public partial string Baujahr { get; set; }

    [ObservableProperty]
    public partial string FeaturesText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAiSummary))]
    public partial string AiSummary { get; set; }

    public bool HasAiSummary => !string.IsNullOrEmpty(AiSummary);

    /// <summary>KI-Ergebnis wurde bereits uebernommen - Resume darf nie erneut ueberschreiben</summary>
    [ObservableProperty]
    public partial bool AnalysisApplied { get; set; }

    #region Analyse-Status (Banner in Schritt 4)

    [ObservableProperty]
    public partial bool IsAnalysisRunning { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAiFailed))]
    public partial string? AiFailedMessage { get; set; }

    public bool HasAiFailed => !string.IsNullOrEmpty(AiFailedMessage);

    #endregion

    private void InitializeDetailsStep()
    {
        Titel = string.Empty;
        Beschreibung = string.Empty;
        Zimmer = string.Empty;
        Wohnflaeche = string.Empty;
        Grundstuecksflaeche = string.Empty;
        Baujahr = string.Empty;
        FeaturesText = string.Empty;
        AiSummary = string.Empty;

        _suppressTypeTracking = true;
        SelectedPropertyTypeItem = PropertyTypes[0]; // "Haus"
        _suppressTypeTracking = false;
    }

    partial void OnSelectedPropertyTypeItemChanged(PropertyTypeItem? value)
    {
        if (!_suppressTypeTracking)
            _userChangedType = true;
    }

    private void OnRunnerStateChanged(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsAnalysisRunning = _runner.State == ListingAnalysisRunState.Running;

            switch (_runner.State)
            {
                case ListingAnalysisRunState.Finished:
                    AiFailedMessage = null;
                    TryApplyAnalysisResult();
                    break;

                case ListingAnalysisRunState.Failed when !AiSkipped:
                    AiFailedMessage = "KI-Analyse nicht verfügbar – bitte Eckdaten manuell erfassen.";
                    break;
            }
        });

    /// <summary>
    /// Uebernimmt das KI-Ergebnis in noch leere Felder (Nutzereingaben gewinnen).
    /// Nur einmal pro Analyse - der AnalysisApplied-Zustand wandert mit in den Entwurf.
    /// </summary>
    private void TryApplyAnalysisResult()
    {
        if (AnalysisApplied || _runner.Result is not { } result)
            return;

        if (string.IsNullOrWhiteSpace(Titel))
            Titel = result.Title ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Beschreibung))
            Beschreibung = result.Description ?? string.Empty;

        if (!_userChangedType)
        {
            _suppressTypeTracking = true;
            SelectedPropertyTypeItem = PropertyTypes.FirstOrDefault(t => t.Value == result.Type) ?? PropertyTypes[0];
            _suppressTypeTracking = false;
        }

        if (string.IsNullOrWhiteSpace(Zimmer))
            Zimmer = result.Rooms?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Wohnflaeche))
            Wohnflaeche = result.LivingAreaSquareMeters?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Grundstuecksflaeche))
            Grundstuecksflaeche = result.PlotAreaSquareMeters?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(Baujahr))
            Baujahr = result.YearBuilt?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(FeaturesText))
            FeaturesText = result.Features != null ? string.Join(", ", result.Features) : string.Empty;

        AiSummary = result.Summary ?? string.Empty;
        AnalysisApplied = true;

        // Zustand sichern, damit ein spaeterer Resume nicht erneut ueberschreibt
        MarkDraftDirty();
        _ = SaveDraftAsync();
    }

    private bool ValidateDetails()
    {
        if (string.IsNullOrWhiteSpace(Titel) || Titel.Trim().Length < 10)
        {
            ErrorMessage = "Titel muss mindestens 10 Zeichen lang sein";
            return false;
        }

        if (string.IsNullOrWhiteSpace(Beschreibung) || Beschreibung.Trim().Length < 50)
        {
            ErrorMessage = "Beschreibung muss mindestens 50 Zeichen lang sein";
            return false;
        }

        return TryParseOptionalInt(Zimmer, "Zimmer", out _)
            && TryParseOptionalInt(Wohnflaeche, "Wohnfläche", out _)
            && TryParseOptionalInt(Grundstuecksflaeche, "Grundstücksfläche", out _)
            && TryParseOptionalInt(Baujahr, "Baujahr", out _);
    }

    private bool TryParseOptionalInt(string value, string fieldName, out int? result)
    {
        result = null;
        if (string.IsNullOrWhiteSpace(value))
            return true;

        if (!int.TryParse(value, out var parsed) || parsed < 0)
        {
            ErrorMessage = $"Bitte geben Sie einen gültigen Wert für \"{fieldName}\" ein";
            return false;
        }

        result = parsed;
        return true;
    }
}
