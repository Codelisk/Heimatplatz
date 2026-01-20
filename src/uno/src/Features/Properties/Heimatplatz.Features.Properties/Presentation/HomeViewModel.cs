using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Auth.Contracts.Interfaces;
using Heimatplatz.Features.Properties.Contracts.Models;
using Heimatplatz.Features.Properties.Models;
using Microsoft.UI.Xaml;
using Uno.Extensions.Navigation;

// ReSharper disable InconsistentNaming

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die HomePage
/// </summary>
public partial class HomeViewModel : ObservableObject
{
    private readonly IAuthService _authService;
    private readonly INavigator _navigator;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string? _busyMessage;

    [ObservableProperty]
    private ObservableCollection<PropertyListItemDto> _properties = new();

    [ObservableProperty]
    private bool _isAllSelected = true;

    [ObservableProperty]
    private bool _isHausSelected;

    [ObservableProperty]
    private bool _isGrundstueckSelected;

    [ObservableProperty]
    private bool _isZwangsversteigerungSelected;

    private AgeFilter _selectedAgeFilter = AgeFilter.Alle;
    public AgeFilter SelectedAgeFilter
    {
        get => _selectedAgeFilter;
        set
        {
            if (SetProperty(ref _selectedAgeFilter, value))
            {
                ApplyFilters();
            }
        }
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsNotAuthenticated))]
    private bool _isAuthenticated;

    /// <summary>
    /// Inverse von IsAuthenticated fuer XAML-Binding
    /// </summary>
    public bool IsNotAuthenticated => !IsAuthenticated;

    [ObservableProperty]
    private string? _userFullName;

    [ObservableProperty]
    private string? _userInitials;

    [ObservableProperty]
    private string? _userFirstInitial;

    [ObservableProperty]
    private string? _userDisplayName;

    private List<string> _selectedOrte = new();
    public List<string> SelectedOrte
    {
        get => _selectedOrte;
        set
        {
            if (SetProperty(ref _selectedOrte, value))
            {
                ApplyFilters();
            }
        }
    }

    /// <summary>
    /// Hierarchische Bezirk/Ort-Struktur für OÖ
    /// </summary>
    public List<BezirkModel> Bezirke { get; } =
    [
        new BezirkModel("Linz-Land", "Traun", "Leonding", "Ansfelden", "Pasching", "Hörsching"),
        new BezirkModel("Linz-Stadt", "Linz", "Urfahr"),
        new BezirkModel("Wels-Land", "Wels", "Marchtrenk", "Gunskirchen"),
        new BezirkModel("Steyr-Land", "Steyr", "Sierning", "Garsten"),
    ];

    public Visibility IsEmpty => Properties.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public HomeViewModel(IAuthService authService, INavigator navigator)
    {
        _authService = authService;
        _navigator = navigator;
        _authService.AuthenticationStateChanged += OnAuthenticationStateChanged;

        UpdateAuthState();
        LoadTestData();
    }

    private void OnAuthenticationStateChanged(object? sender, bool isAuthenticated)
    {
        UpdateAuthState();
    }

    private void UpdateAuthState()
    {
        IsAuthenticated = _authService.IsAuthenticated;
        UserFullName = _authService.UserFullName;
        UserInitials = GetInitials(_authService.UserFullName);
        UserFirstInitial = GetFirstInitial(_authService.UserFullName);
        UserDisplayName = GetDisplayName(_authService.UserFullName);
    }

    private static string? GetInitials(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        if (parts.Length == 1)
            return parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant();

        return $"{parts[0][0]}{parts[^1][0]}".ToUpperInvariant();
    }

    /// <summary>
    /// Erstes Initial fuer Monogramm-Anzeige (z.B. "M")
    /// </summary>
    private static string? GetFirstInitial(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        return fullName.Trim()[0].ToString().ToUpperInvariant();
    }

    /// <summary>
    /// Abgekuerzter Anzeigename fuer elegante Darstellung (z.B. "M. Schmidt")
    /// </summary>
    private static string? GetDisplayName(string? fullName)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            return null;

        var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return null;

        if (parts.Length == 1)
            return parts[0];

        // Format: "M. Nachname"
        return $"{parts[0][0]}. {parts[^1]}";
    }

    [RelayCommand]
    private void Logout()
    {
        _authService.ClearAuthentication();
    }

    [RelayCommand]
    private async Task NavigateToAddPropertyAsync()
    {
        await _navigator.NavigateViewModelAsync<AddPropertyViewModel>(this);
    }

    [RelayCommand]
    private async Task NavigateToFavoritesAsync()
    {
        await _navigator.NavigateViewModelAsync<FavoritesViewModel>(this);
    }

    [RelayCommand]
    private async Task NavigateToBlockedAsync()
    {
        await _navigator.NavigateViewModelAsync<BlockedViewModel>(this);
    }

    partial void OnIsAllSelectedChanged(bool value)
    {
        if (value)
        {
            IsHausSelected = false;
            IsGrundstueckSelected = false;
            IsZwangsversteigerungSelected = false;
            ApplyFilters();
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
            ApplyFilters();
        }
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
            ApplyFilters();
        }
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
            ApplyFilters();
        }
    }

    private void ApplyFilters()
    {
        System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Called. SelectedOrte.Count = {SelectedOrte.Count}");
        var filtered = _allProperties.AsEnumerable();

        // City filter (Multi-Select)
        if (SelectedOrte.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by cities: {string.Join(", ", SelectedOrte)}");
            filtered = filtered.Where(p => SelectedOrte.Contains(p.City));
        }

        // Age filter
        if (SelectedAgeFilter != AgeFilter.Alle)
        {
            var cutoffDate = SelectedAgeFilter switch
            {
                AgeFilter.EinTag => DateTime.Now.AddDays(-1),
                AgeFilter.EineWoche => DateTime.Now.AddDays(-7),
                AgeFilter.EinMonat => DateTime.Now.AddMonths(-1),
                AgeFilter.EinJahr => DateTime.Now.AddYears(-1),
                _ => DateTime.MinValue
            };
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by age: {SelectedAgeFilter}, cutoff: {cutoffDate}");
            filtered = filtered.Where(p => p.CreatedAt >= cutoffDate);
        }

        // Type filter (Multi-Select mit OR-Logik)
        var selectedTypes = new List<PropertyType>();
        if (IsHausSelected) selectedTypes.Add(PropertyType.House);
        if (IsGrundstueckSelected) selectedTypes.Add(PropertyType.Land);
        if (IsZwangsversteigerungSelected) selectedTypes.Add(PropertyType.Foreclosure);

        if (selectedTypes.Count > 0)
        {
            System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Filtering by types: {string.Join(", ", selectedTypes)}");
            filtered = filtered.Where(p => selectedTypes.Contains(p.Type));
        }

        Properties.Clear();
        var filteredList = filtered.ToList();
        System.Diagnostics.Debug.WriteLine($"[ApplyFilters] Result count: {filteredList.Count}");
        foreach (var property in filteredList)
        {
            Properties.Add(property);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }

    private List<PropertyListItemDto> _allProperties = new();

    private void LoadTestData()
    {
        var now = DateTime.Now;

        _allProperties = new List<PropertyListItemDto>
        {
            // Created today
            new(Guid.NewGuid(), "Einfamilienhaus in Linz-Urfahr", "Hauptstrasse 15", "Linz", 349000, 145, 520, 5, PropertyType.House, SellerType.Makler, "Mustermann Immobilien",
                ["https://picsum.photos/seed/haus1a/800/600", "https://picsum.photos/seed/haus1b/800/600", "https://picsum.photos/seed/haus1c/800/600"],
                now.AddHours(-2)),

            // Created yesterday
            new(Guid.NewGuid(), "Modernes Reihenhaus in Wels", "Ringstrasse 42", "Wels", 289000, 120, 180, 4, PropertyType.House, SellerType.Privat, "Familie Huber",
                ["https://picsum.photos/seed/haus2a/800/600", "https://picsum.photos/seed/haus2b/800/600"],
                now.AddHours(-20)),

            // Created 5 days ago
            new(Guid.NewGuid(), "Familienhaus in Steyr", "Bahnhofstrasse 67", "Steyr", 315000, 135, 450, 5, PropertyType.House, SellerType.Makler, "Immobilien Steyr",
                ["https://picsum.photos/seed/haus3a/800/600", "https://picsum.photos/seed/haus3b/800/600", "https://picsum.photos/seed/haus3c/800/600", "https://picsum.photos/seed/haus3d/800/600"],
                now.AddDays(-5)),

            // Created 2 weeks ago
            new(Guid.NewGuid(), "Baugrundstück in Wels", "Neubaugebiet Sued", "Wels", 189000, null, 850, null, PropertyType.Land, SellerType.Privat, "Familie Mueller",
                ["https://picsum.photos/seed/grund1a/800/600", "https://picsum.photos/seed/grund1b/800/600"],
                now.AddDays(-14)),

            // Created 2 months ago
            new(Guid.NewGuid(), "Sonniges Baugrundstück Linz-Land", "Am Sonnenhang 12", "Leonding", 245000, null, 720, null, PropertyType.Land, SellerType.Makler, "Grund & Boden OOe",
                ["https://picsum.photos/seed/grund2a/800/600", "https://picsum.photos/seed/grund2b/800/600", "https://picsum.photos/seed/grund2c/800/600"],
                now.AddMonths(-2)),

            // Created 6 months ago
            new(Guid.NewGuid(), "Zwangsversteigerung: Haus in Traun", "Industriestrasse 45", "Traun", 185000, 110, 380, 4, PropertyType.Foreclosure, SellerType.Makler, "Bezirksgericht Linz",
                ["https://picsum.photos/seed/zwang1a/800/600", "https://picsum.photos/seed/zwang1b/800/600"],
                now.AddMonths(-6)),
        };

        ApplyFilters();
    }
}
