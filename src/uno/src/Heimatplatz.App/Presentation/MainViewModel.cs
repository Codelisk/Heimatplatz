using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Auth.Contracts.Interfaces;

namespace Heimatplatz.App.Presentation;

/// <summary>
/// ViewModel for MainPage - Region Navigation Container
/// MainPage acts as a transparent container for region-based navigation.
/// Nested routes (Home, MyProperties, etc.) are displayed in the ContentRegion.
/// </summary>
public partial class MainViewModel : PageViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private bool _isAuthenticated;

    public MainViewModel(BaseServices baseServices, IAuthService authService) : base(baseServices)
    {
        _authService = authService;
        IsAuthenticated = _authService.IsAuthenticated;

        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        IsAuthenticated = isAuthenticated;
    }
}
