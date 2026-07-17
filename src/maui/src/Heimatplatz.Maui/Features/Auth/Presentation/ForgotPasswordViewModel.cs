using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Maui.ApiClient.Generated;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer "Passwort vergessen": fordert die Reset-Mail an.
/// Die Antwort des Servers ist bewusst immer generisch (kein Rueckschluss darauf,
/// ob ein Konto existiert); das neue Passwort wird ueber den Link in der Mail
/// im Web-Frontend gesetzt.
/// </summary>
[ShellMap<ForgotPasswordPage>("ForgotPassword")]
public partial class ForgotPasswordViewModel : ObservableObject
{
    private readonly IMediator _mediator;
    private readonly INavigator _navigator;
    private readonly ILogger<ForgotPasswordViewModel> _logger;

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string Email { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasError))]
    public partial string? ErrorMessage { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSuccess))]
    public partial string? SuccessMessage { get; set; }

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public bool HasSuccess => !string.IsNullOrEmpty(SuccessMessage);

    public ForgotPasswordViewModel(
        IMediator mediator,
        INavigator navigator,
        ILogger<ForgotPasswordViewModel> logger)
    {
        _mediator = mediator;
        _navigator = navigator;
        _logger = logger;

        Email = string.Empty;
    }

    [RelayCommand]
    private async Task SendResetLinkAsync()
    {
        ErrorMessage = null;
        SuccessMessage = null;

        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Bitte geben Sie Ihre E-Mail-Adresse ein.";
            return;
        }

        IsBusy = true;
        try
        {
            var (_, result) = await _mediator.Request(new ForgotPasswordHttpRequest
            {
                Body = new ForgotPasswordRequest { Email = Email }
            });

            SuccessMessage = result?.Message
                ?? "Falls ein Konto mit dieser E-Mail-Adresse existiert, haben wir Ihnen einen Link zum Zurücksetzen gesendet.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Passwort-Reset-Anforderung fehlgeschlagen");
            ErrorMessage = "Die Anfrage ist fehlgeschlagen. Bitte prüfen Sie Ihre Internetverbindung und versuchen Sie es erneut.";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private Task GoBackAsync() => _navigator.GoBack();
}
