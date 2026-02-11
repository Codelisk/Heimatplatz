#if DEBUG
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Core.ApiClient.Generated;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;
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
    private readonly IMediator _mediator;

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
        IMediator mediator)
    {
        _navigator = navigator;
        _logger = logger;
        _mediator = mediator;
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

            var (_, result) = await _mediator.Request(new SendTestPushHttpRequest
            {
                Body = new SendTestPushRequest
                {
                    Title = Title,
                    Body = Body
                }
            });

            ResultMessage = result?.Message ?? "Push gesendet!";
            _logger.LogInformation("[TestPush] Erfolg: {Message}", ResultMessage);
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
}
#endif
