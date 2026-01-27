using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using UnoFramework.Contracts.Navigation;
using UnoFramework.Contracts.Pages;

namespace Heimatplatz.Features.Auth.Presentation;

/// <summary>
/// ViewModel fuer die Mein Profil Seite.
/// Zeigt alle verfuegbaren Benutzerdaten aus dem IAuthService an.
/// </summary>
public partial class UserProfileViewModel : ObservableObject, IPageInfo, INavigationAware
{
    private readonly IAuthService _authService;
    private readonly ILogger<UserProfileViewModel> _logger;

    [ObservableProperty]
    private string _userFullName = string.Empty;

    [ObservableProperty]
    private string _userInitials = string.Empty;

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _roleBadgeText = string.Empty;

    [ObservableProperty]
    private bool _isSeller;

    [ObservableProperty]
    private bool _isBuyer;

    #region IPageInfo Implementation

    public PageType PageType => PageType.Detail;
    public string PageTitle => "Mein Profil";
    public Type? MainHeaderViewModel => null;

    #endregion

    public UserProfileViewModel(
        IAuthService authService,
        ILogger<UserProfileViewModel> logger)
    {
        _authService = authService;
        _logger = logger;

        LoadUserData();
    }

    private void LoadUserData()
    {
        if (!_authService.IsAuthenticated)
        {
            _logger.LogWarning("[UserProfile] Nicht authentifiziert");
            return;
        }

        UserFullName = _authService.UserFullName ?? string.Empty;
        UserEmail = _authService.UserEmail ?? string.Empty;
        UserInitials = GetInitials(_authService.UserFullName);
        IsSeller = _authService.IsSeller;
        IsBuyer = _authService.IsBuyer;

        // Role badge text
        RoleBadgeText = (IsSeller, IsBuyer) switch
        {
            (true, true) => "Kaeufer & Verkaeufer",
            (true, false) => "Verkaeufer",
            (false, true) => "Kaeufer",
            _ => string.Empty
        };

        _logger.LogInformation("[UserProfile] Benutzerdaten geladen: {Name}, {Email}", UserFullName, UserEmail);
    }

    private static string GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return "?";

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return "?";

        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    #region INavigationAware Implementation

    public void OnNavigatedTo(object? parameter)
    {
        _logger.LogDebug("[UserProfile] OnNavigatedTo");
        LoadUserData();
    }

    public void OnNavigatedFrom()
    {
        _logger.LogDebug("[UserProfile] OnNavigatedFrom");
    }

    #endregion
}
