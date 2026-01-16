using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Anmeldeseite
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public partial class LoginViewModel : ObservableObject
{
    private readonly IMediator _mediator;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _passwort = string.Empty;

    [ObservableProperty]
    private string? _errorMessage;

    public LoginViewModel(IMediator mediator)
    {
        _mediator = mediator;
    }

    public bool CanLogin =>
        !string.IsNullOrWhiteSpace(Email) &&
        !string.IsNullOrWhiteSpace(Passwort);

    partial void OnEmailChanged(string value) => OnPropertyChanged(nameof(CanLogin));
    partial void OnPasswortChanged(string value) => OnPropertyChanged(nameof(CanLogin));

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (!CanLogin)
        {
            ErrorMessage = GetValidationError();
            return;
        }

        ErrorMessage = null;
        IsBusy = true;
        BusyMessage = "Anmeldung wird durchgeführt...";

        try
        {
            // TODO: Implementiere Login-API-Aufruf wenn verfuegbar
            // Beispiel:
            // var response = await _mediator.Request(new LoginHttpRequest
            // {
            //     Body = new LoginRequest
            //     {
            //         Email = Email,
            //         Passwort = Passwort
            //     }
            // });

            // Simulierte Verzoegerung fuer Demo
            await Task.Delay(1000);

            // Erfolgreiche Anmeldung simulieren
            // In der echten Implementierung: Token speichern und navigieren

            // Formular zuruecksetzen
            Email = string.Empty;
            Passwort = string.Empty;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private string GetValidationError()
    {
        if (string.IsNullOrWhiteSpace(Email))
            return "Bitte geben Sie Ihre E-Mail-Adresse ein.";
        if (string.IsNullOrWhiteSpace(Passwort))
            return "Bitte geben Sie Ihr Passwort ein.";
        return string.Empty;
    }
}
