#if DEBUG
using System.Net.Http.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Features.Debug.Presentation;

/// <summary>
/// ViewModel fuer die Test Push Page
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class TestPushViewModel : ObservableObject
{
    private readonly INavigator _navigator;
    private readonly ILogger<TestPushViewModel> _logger;
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    [ObservableProperty]
    private string _title = "Test Notification";

    [ObservableProperty]
    private string _body = "Dies ist eine Test-Push-Nachricht von der Debug-Seite.";

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private bool _hasResult;

    [ObservableProperty]
    private string _resultMessage = string.Empty;

    public TestPushViewModel(
        INavigator navigator,
        ILogger<TestPushViewModel> logger,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory)
    {
        _navigator = navigator;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        // Get base URL from configuration (same as Mediator HTTP config)
        _baseUrl = configuration["Mediator:Http:Heimatplatz.Core.ApiClient.Generated.*"]
            ?? "http://localhost:5292";

        _logger.LogInformation("[TestPush] Base URL: {BaseUrl}", _baseUrl);
    }

    [RelayCommand]
    private async Task SendPushAsync()
    {
        if (string.IsNullOrWhiteSpace(Title) || string.IsNullOrWhiteSpace(Body))
        {
            ResultMessage = "Bitte Titel und Nachricht eingeben.";
            HasResult = true;
            return;
        }

        IsLoading = true;
        HasResult = false;

        try
        {
            _logger.LogInformation("[TestPush] Sende Push: Title={Title}, Body={Body}", Title, Body);

            var request = new SendTestPushRequest(Title, Body);
            var response = await _httpClient.PostAsJsonAsync(
                $"{_baseUrl}/api/notifications/test-push",
                request);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<SendTestPushResponse>();
                ResultMessage = result?.Message ?? "Push gesendet!";
                _logger.LogInformation("[TestPush] Erfolg: {Message}", ResultMessage);
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                ResultMessage = $"Fehler: {response.StatusCode} - {errorContent}";
                _logger.LogWarning("[TestPush] Fehler: {StatusCode} - {Content}", response.StatusCode, errorContent);
            }
        }
        catch (Exception ex)
        {
            ResultMessage = $"Exception: {ex.Message}";
            _logger.LogError(ex, "[TestPush] Exception beim Senden");
        }
        finally
        {
            IsLoading = false;
            HasResult = true;
        }
    }

    [RelayCommand]
    private async Task GoBackAsync()
    {
        await _navigator.NavigateBackAsync(this);
    }

    // DTOs for API call
    private record SendTestPushRequest(string Title, string Body);
    private record SendTestPushResponse(int SentCount, string Message);
}
#endif
