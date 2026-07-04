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
using MauiPermissions = Microsoft.Maui.ApplicationModel.Permissions;

namespace Heimatplatz.Maui.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die KI-gestuetzte Inseratserstellung (Primaerweg auf Android/iOS-Phones):
/// 1. Fotos/Videos aufnehmen bzw. auswaehlen
/// 2. Beschreibung diktieren (Shiny.Speech)
/// 3. Eckdaten manuell erfassen (Preis, Adresse, Ort - bewusst NICHT per KI)
/// 4. KI-Analyse am Server (Upload, Status-Polling)
/// 5. Review der KI-Ergebnisse und Veroeffentlichung
/// </summary>
[ShellMap<AiAddPropertyPage>("AiAddProperty")]
public partial class AiAddPropertyViewModel : ObservableObject, IPageLifecycleAware
{
    private const int MaxPhotos = 20;
    private const int MaxVideos = 3;
    private const int MaxVideoSizeMb = 60;
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan AnalysisTimeout = TimeSpan.FromMinutes(6);

    private readonly IAuthService _authService;
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILocationService _locationService;
    private readonly IDictationService _dictation;
    private readonly ILogger<AiAddPropertyViewModel> _logger;

    private List<LocationGemeindeDto> _municipalities = [];
    private bool _suppressSearch;
    private CancellationTokenSource? _analysisCts;
    private List<string> _uploadedImageUrls = [];
    private List<string> _uploadedVideoUrls = [];

    public List<PropertyTypeItem> PropertyTypes { get; } = PropertyTypeItem.GetAll();

    #region Phasen (0 = Eingabe, 1 = Analyse, 2 = Review)

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInputPhase))]
    [NotifyPropertyChangedFor(nameof(IsAnalyzingPhase))]
    [NotifyPropertyChangedFor(nameof(IsReviewPhase))]
    public partial int Phase { get; set; }

    public bool IsInputPhase => Phase == 0;
    public bool IsAnalyzingPhase => Phase == 1;
    public bool IsReviewPhase => Phase == 2;

    #endregion

    #region Medien

    public ObservableCollection<AiMediaItem> Media { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasMedia))]
    [NotifyPropertyChangedFor(nameof(HasNoMedia))]
    [NotifyPropertyChangedFor(nameof(MediaCountText))]
    public partial int MediaCount { get; set; }

    public bool HasMedia => MediaCount > 0;
    public bool HasNoMedia => MediaCount == 0;

    public string MediaCountText
    {
        get
        {
            var photos = Media.Count(m => m.IsPhoto);
            var videos = Media.Count(m => m.IsVideo);
            return videos > 0 ? $"{photos} Fotos · {videos} Videos" : $"{photos} Fotos";
        }
    }

    #endregion

    #region Diktat

    public bool DictationSupported => _dictation.IsSupported;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MicButtonText))]
    [NotifyPropertyChangedFor(nameof(MicHintText))]
    public partial bool IsListening { get; set; }

    public string MicButtonText => IsListening ? "◼" : "🎤";

    public string MicHintText => IsListening
        ? "Aufnahme läuft – tippen zum Stoppen"
        : "Tippen und Immobilie beschreiben";

    [ObservableProperty]
    public partial string DictatedText { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasLivePartial))]
    public partial string LivePartial { get; set; }

    public bool HasLivePartial => !string.IsNullOrEmpty(LivePartial);

    #endregion

    #region Manuelle Felder (bewusst nicht per KI)

    [ObservableProperty]
    public partial string Preis { get; set; }

    [ObservableProperty]
    public partial string Adresse { get; set; }

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

    #endregion

    #region Analyse-Status

    [ObservableProperty]
    public partial string AnalysisStatusText { get; set; }

    [ObservableProperty]
    public partial bool UploadStepDone { get; set; }

    [ObservableProperty]
    public partial bool AnalysisStepDone { get; set; }

    #endregion

    #region Review-Felder (von der KI befuellt, editierbar)

    [ObservableProperty]
    public partial string ReviewTitel { get; set; }

    [ObservableProperty]
    public partial string ReviewBeschreibung { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsReviewHouseType))]
    public partial PropertyTypeItem? ReviewTypItem { get; set; }

    public bool IsReviewHouseType => ReviewTypItem?.Value == PropertyType.House;

    [ObservableProperty]
    public partial string ReviewZimmer { get; set; }

    [ObservableProperty]
    public partial string ReviewWohnflaeche { get; set; }

    [ObservableProperty]
    public partial string ReviewGrundstuecksflaeche { get; set; }

    [ObservableProperty]
    public partial string ReviewBaujahr { get; set; }

    [ObservableProperty]
    public partial string ReviewFeatures { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasAiSummary))]
    public partial string AiSummary { get; set; }

    public bool HasAiSummary => !string.IsNullOrEmpty(AiSummary);

    #endregion

    #region UI-State

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    #endregion

    public AiAddPropertyViewModel(
        IAuthService authService,
        IMediator mediator,
        INavigator navigator,
        ILocationService locationService,
        IDictationService dictation,
        ILogger<AiAddPropertyViewModel> logger)
    {
        _authService = authService;
        _mediator = mediator;
        _navigator = navigator;
        _locationService = locationService;
        _dictation = dictation;
        _logger = logger;

        DictatedText = string.Empty;
        LivePartial = string.Empty;
        Preis = string.Empty;
        Adresse = string.Empty;
        OrtSearchText = string.Empty;
        OrtSuggestions = [];
        SelectedOrtText = string.Empty;
        AnalysisStatusText = string.Empty;
        ReviewTitel = string.Empty;
        ReviewBeschreibung = string.Empty;
        ReviewZimmer = string.Empty;
        ReviewWohnflaeche = string.Empty;
        ReviewGrundstuecksflaeche = string.Empty;
        ReviewBaujahr = string.Empty;
        ReviewFeatures = string.Empty;
        AiSummary = string.Empty;

        _dictation.PartialResult += OnDictationPartial;
        _dictation.FinalResult += OnDictationFinal;
        _dictation.Failed += OnDictationFailed;
        _dictation.Stopped += OnDictationStopped;
    }

    #region IPageLifecycleAware

    public void OnAppearing()
    {
        if (_municipalities.Count == 0)
            _ = LoadMunicipalitiesAsync();
    }

    public void OnDisappearing()
    {
        if (IsListening)
            _ = _dictation.StopAsync();
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
            _logger.LogError(ex, "[AiAddProperty] Fehler beim Laden der Orte");
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

    #region Medien

    [RelayCommand]
    private async Task TakePhotoAsync()
    {
        try
        {
            if (!await EnsurePhotoCapacityAsync())
                return;

            var cameraStatus = await MauiPermissions.RequestAsync<MauiPermissions.Camera>();
            if (cameraStatus != PermissionStatus.Granted)
            {
                ErrorMessage = "Kamera-Berechtigung wurde nicht erteilt.";
                return;
            }

            var file = await MediaPicker.Default.CapturePhotoAsync();
            if (file == null)
                return;

            await AddMediaFileAsync(file, isVideo: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler bei Foto-Aufnahme");
            ErrorMessage = $"Fehler bei der Aufnahme: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PickPhotosAsync()
    {
        try
        {
            if (!await EnsurePhotoCapacityAsync())
                return;

            var files = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Fotos auswählen"
            });
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (Media.Count(m => m.IsPhoto) >= MaxPhotos)
                {
                    ErrorMessage = $"Maximal {MaxPhotos} Fotos erlaubt";
                    break;
                }

                await AddMediaFileAsync(file, isVideo: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler bei Foto-Auswahl");
            ErrorMessage = $"Fehler bei der Auswahl: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RecordVideoAsync()
    {
        try
        {
            if (!await EnsureVideoCapacityAsync())
                return;

            var cameraStatus = await MauiPermissions.RequestAsync<MauiPermissions.Camera>();
            var micStatus = await MauiPermissions.RequestAsync<MauiPermissions.Microphone>();
            if (cameraStatus != PermissionStatus.Granted || micStatus != PermissionStatus.Granted)
            {
                ErrorMessage = "Kamera-/Mikrofon-Berechtigung wurde nicht erteilt.";
                return;
            }

            var file = await MediaPicker.Default.CaptureVideoAsync();
            if (file == null)
                return;

            await AddMediaFileAsync(file, isVideo: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler bei Video-Aufnahme");
            ErrorMessage = $"Fehler bei der Video-Aufnahme: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task PickVideoAsync()
    {
        try
        {
            if (!await EnsureVideoCapacityAsync())
                return;

            var files = await MediaPicker.Default.PickVideosAsync(new MediaPickerOptions
            {
                Title = "Videos auswählen"
            });
            if (files == null)
                return;

            foreach (var file in files)
            {
                if (Media.Count(m => m.IsVideo) >= MaxVideos)
                {
                    ErrorMessage = $"Maximal {MaxVideos} Videos erlaubt";
                    break;
                }

                await AddMediaFileAsync(file, isVideo: true);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler bei Video-Auswahl");
            ErrorMessage = $"Fehler bei der Auswahl: {ex.Message}";
        }
    }

    [RelayCommand]
    private void RemoveMedia(AiMediaItem item)
    {
        Media.Remove(item);
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));
    }

    private Task<bool> EnsurePhotoCapacityAsync()
    {
        if (Media.Count(m => m.IsPhoto) >= MaxPhotos)
        {
            ErrorMessage = $"Maximal {MaxPhotos} Fotos erlaubt";
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    private Task<bool> EnsureVideoCapacityAsync()
    {
        if (Media.Count(m => m.IsVideo) >= MaxVideos)
        {
            ErrorMessage = $"Maximal {MaxVideos} Videos erlaubt";
            return Task.FromResult(false);
        }
        return Task.FromResult(true);
    }

    private async Task AddMediaFileAsync(FileResult file, bool isVideo)
    {
        using var stream = await file.OpenReadAsync();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        if (isVideo && bytes.Length > MaxVideoSizeMb * 1024L * 1024L)
        {
            ErrorMessage = $"Video '{file.FileName}' ist zu groß (max. {MaxVideoSizeMb} MB).";
            return;
        }

        var contentType = string.IsNullOrEmpty(file.ContentType)
            ? GuessContentType(file.FileName, isVideo)
            : file.ContentType;

        Media.Add(new AiMediaItem(file.FileName, contentType, bytes, isVideo));
        MediaCount = Media.Count;
        OnPropertyChanged(nameof(MediaCountText));
        ErrorMessage = null;
    }

    private static string GuessContentType(string fileName, bool isVideo)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".webp" => "image/webp",
            ".heic" => "image/heic",
            ".mov" => "video/quicktime",
            ".webm" => "video/webm",
            ".m4v" => "video/x-m4v",
            ".mp4" => "video/mp4",
            _ => isVideo ? "video/mp4" : "image/jpeg"
        };
    }

    #endregion

    #region Diktat

    [RelayCommand]
    private async Task ToggleDictationAsync()
    {
        try
        {
            if (IsListening)
            {
                await _dictation.StopAsync();
                return;
            }

            if (!_dictation.IsSupported)
            {
                ErrorMessage = "Diktat ist auf diesem Gerät nicht verfügbar.";
                return;
            }

            if (!await _dictation.RequestPermissionAsync())
            {
                ErrorMessage = "Mikrofon-Berechtigung wurde nicht erteilt.";
                return;
            }

            ErrorMessage = null;
            await _dictation.StartAsync();
            IsListening = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler beim Diktat");
            ErrorMessage = $"Fehler beim Diktat: {ex.Message}";
            IsListening = false;
        }
    }

    private void OnDictationPartial(object? sender, string text) =>
        MainThread.BeginInvokeOnMainThread(() => LivePartial = text);

    private void OnDictationFinal(object? sender, string text) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            DictatedText = string.IsNullOrWhiteSpace(DictatedText)
                ? text
                : $"{DictatedText.TrimEnd()} {text}";
            LivePartial = string.Empty;
        });

    private void OnDictationFailed(object? sender, string message) =>
        MainThread.BeginInvokeOnMainThread(() => ErrorMessage = message);

    private void OnDictationStopped(object? sender, EventArgs e) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsListening = false;
            LivePartial = string.Empty;
        });

    #endregion

    #region KI-Analyse

    [RelayCommand]
    private async Task StartAiCreationAsync()
    {
        ErrorMessage = null;

        // Validierung der manuellen Eckdaten
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

        var photos = Media.Where(m => m.IsPhoto).ToList();
        if (photos.Count == 0 && string.IsNullOrWhiteSpace(DictatedText))
        {
            ErrorMessage = "Bitte fügen Sie mindestens ein Foto hinzu oder diktieren Sie eine Beschreibung";
            return;
        }

        if (IsListening)
            await _dictation.StopAsync();

        Phase = 1;
        UploadStepDone = false;
        AnalysisStepDone = false;
        _analysisCts = new CancellationTokenSource(AnalysisTimeout);

        try
        {
            // 1) Medien einzeln hochladen (kleine Request-Bodies)
            _uploadedImageUrls = [];
            _uploadedVideoUrls = [];
            var allMedia = Media.ToList();

            for (var i = 0; i < allMedia.Count; i++)
            {
                _analysisCts.Token.ThrowIfCancellationRequested();
                AnalysisStatusText = $"Medien werden hochgeladen ({i + 1}/{allMedia.Count})…";

                var item = allMedia[i];
                var (_, uploadResult) = await _mediator.Request(
                    new UploadListingMediaHttpRequest
                    {
                        Body = new UploadListingMediaRequest
                        {
                            Media =
                            [
                                new Base64MediaData
                                {
                                    FileName = item.FileName,
                                    ContentType = item.ContentType,
                                    Base64Data = item.ToBase64()
                                }
                            ]
                        }
                    }, _analysisCts.Token);

                if (uploadResult?.ImageUrls != null)
                    _uploadedImageUrls.AddRange(uploadResult.ImageUrls);
                if (uploadResult?.VideoUrls != null)
                    _uploadedVideoUrls.AddRange(uploadResult.VideoUrls);
            }

            UploadStepDone = true;
            AnalysisStatusText = "Die KI analysiert Ihre Immobilie…";

            // 2) Analyse starten
            var (_, startResult) = await _mediator.Request(
                new StartListingAnalysisHttpRequest
                {
                    Body = new StartListingAnalysisRequest
                    {
                        ImageUrls = _uploadedImageUrls,
                        VideoUrls = _uploadedVideoUrls,
                        DictatedText = string.IsNullOrWhiteSpace(DictatedText) ? null : DictatedText.Trim()
                    }
                }, _analysisCts.Token);

            if (startResult == null)
                throw new InvalidOperationException("Analyse konnte nicht gestartet werden.");

            // 3) Status-Polling bis Finished/Failed
            var analysisId = startResult.AnalysisId;
            GetListingAnalysisResponse? status;

            while (true)
            {
                await Task.Delay(PollInterval, _analysisCts.Token);

                var (_, pollResult) = await _mediator.Request(
                    new GetListingAnalysisHttpRequest { AnalysisId = analysisId },
                    _analysisCts.Token);

                status = pollResult;

                if (status == null)
                    continue;

                if (status.Status == ListingAnalysisStatus.Finished || status.Status == ListingAnalysisStatus.Failed)
                    break;

                AnalysisStatusText = status.Status == ListingAnalysisStatus.InProgress
                    ? "Die KI analysiert Ihre Immobilie…"
                    : "Analyse in der Warteschlange…";
            }

            if (status.Status == ListingAnalysisStatus.Failed || status.Result == null)
            {
                ErrorMessage = string.IsNullOrEmpty(status.ErrorMessage)
                    ? "Die KI-Analyse ist fehlgeschlagen. Bitte versuchen Sie es erneut."
                    : $"KI-Analyse fehlgeschlagen: {status.ErrorMessage}";
                Phase = 0;
                return;
            }

            AnalysisStepDone = true;
            ApplyAnalysisResult(status.Result);
            Phase = 2;
        }
        catch (OperationCanceledException)
        {
            ErrorMessage = "Die KI-Analyse wurde abgebrochen oder hat zu lange gedauert.";
            Phase = 0;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler bei der KI-Analyse");
            ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
            Phase = 0;
        }
        finally
        {
            _analysisCts?.Dispose();
            _analysisCts = null;
        }
    }

    [RelayCommand]
    private void CancelAnalysis() => _analysisCts?.Cancel();

    private void ApplyAnalysisResult(ExtractedListingData result)
    {
        ReviewTitel = result.Title ?? string.Empty;
        ReviewBeschreibung = result.Description ?? string.Empty;
        ReviewTypItem = PropertyTypes.FirstOrDefault(t => t.Value == result.Type) ?? PropertyTypes[0];
        ReviewZimmer = result.Rooms?.ToString() ?? string.Empty;
        ReviewWohnflaeche = result.LivingAreaSquareMeters?.ToString() ?? string.Empty;
        ReviewGrundstuecksflaeche = result.PlotAreaSquareMeters?.ToString() ?? string.Empty;
        ReviewBaujahr = result.YearBuilt?.ToString() ?? string.Empty;
        ReviewFeatures = result.Features != null ? string.Join(", ", result.Features) : string.Empty;
        AiSummary = result.Summary ?? string.Empty;
    }

    #endregion

    #region Veroeffentlichen

    [RelayCommand]
    private async Task PublishAsync()
    {
        ErrorMessage = null;

        if (string.IsNullOrWhiteSpace(ReviewTitel) || ReviewTitel.Length < 10)
        {
            ErrorMessage = "Titel muss mindestens 10 Zeichen lang sein";
            return;
        }

        if (string.IsNullOrWhiteSpace(ReviewBeschreibung) || ReviewBeschreibung.Length < 50)
        {
            ErrorMessage = "Beschreibung muss mindestens 50 Zeichen lang sein";
            return;
        }

        if (!decimal.TryParse(Preis, out var preisValue) || preisValue <= 0)
        {
            ErrorMessage = "Bitte geben Sie einen gültigen Preis ein";
            return;
        }

        IsBusy = true;
        var publishSucceeded = false;

        try
        {
            int? zimmer = int.TryParse(ReviewZimmer, out var z) ? z : null;
            int? wohnflaeche = int.TryParse(ReviewWohnflaeche, out var wf) ? wf : null;
            int? grundstueck = int.TryParse(ReviewGrundstuecksflaeche, out var gs) ? gs : null;
            int? baujahr = int.TryParse(ReviewBaujahr, out var bj) ? bj : null;

            var features = ReviewFeatures
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            await _mediator.Request(new CreatePropertyHttpRequest
            {
                Body = new CreatePropertyRequest
                {
                    Title = ReviewTitel.Trim(),
                    Address = Adresse.Trim(),
                    MunicipalityId = SelectedGemeindeId!.Value,
                    Price = (double)preisValue,
                    Type = ReviewTypItem?.Value ?? PropertyType.House,
                    SellerType = SellerType.Private,
                    SellerName = _authService.UserFullName ?? "Unbekannt",
                    Description = ReviewBeschreibung.Trim(),
                    LivingAreaSquareMeters = wohnflaeche,
                    PlotAreaSquareMeters = grundstueck,
                    Rooms = zimmer,
                    YearBuilt = baujahr,
                    Features = features.Count > 0 ? features : null,
                    ImageUrls = _uploadedImageUrls.Count > 0 ? _uploadedImageUrls : null
                }
            });

            publishSucceeded = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AiAddProperty] Fehler beim Veroeffentlichen");
            ErrorMessage = $"Ein Fehler ist aufgetreten: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }

        if (publishSucceeded)
        {
            _logger.LogInformation("[AiAddProperty] Inserat veroeffentlicht, Navigation zu MyProperties");
            await _navigator.NavigateTo("MyProperties");
        }
    }

    [RelayCommand]
    private void BackToInput() => Phase = 0;

    [RelayCommand]
    private Task Cancel() => _navigator.GoBack();

    /// <summary>Sekundaerweg: manuelle Erfassung</summary>
    [RelayCommand]
    private Task SwitchToManual() => _navigator.NavigateTo(PropertyCreationRoutes.Manual);

    #endregion
}
