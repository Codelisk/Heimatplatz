using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.UI.Xaml;

namespace Heimatplatz.Features.Properties.Presentation;

/// <summary>
/// ViewModel fuer die HomePage
/// </summary>
public partial class HomeViewModel : ObservableObject
{
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

    [ObservableProperty]
    private string? _selectedOrt;

    public List<string> Orte { get; } = ["Alle Orte", "Linz", "Wels", "Steyr", "Leonding", "Traun"];

    public Visibility IsEmpty => Properties.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public HomeViewModel()
    {
        SelectedOrt = "Alle Orte";
        // TODO: API-Integration - vorerst Testdaten laden
        LoadTestData();
    }

    partial void OnSelectedOrtChanged(string? value)
    {
        ApplyFilters();
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
        var filtered = _allProperties.AsEnumerable();

        // Ort-Filter
        if (!string.IsNullOrEmpty(SelectedOrt) && SelectedOrt != "Alle Orte")
        {
            filtered = filtered.Where(p => p.Ort == SelectedOrt);
        }

        // Typ-Filter
        if (IsHausSelected)
            filtered = filtered.Where(p => p.Typ == PropertyType.Haus);
        else if (IsGrundstueckSelected)
            filtered = filtered.Where(p => p.Typ == PropertyType.Grundstueck);
        else if (IsZwangsversteigerungSelected)
            filtered = filtered.Where(p => p.Typ == PropertyType.Zwangsversteigerung);

        Properties.Clear();
        foreach (var property in filtered)
        {
            Properties.Add(property);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }

    private List<PropertyListItemDto> _allProperties = new();

    private void LoadTestData()
    {
        _allProperties = new List<PropertyListItemDto>
        {
            new(Guid.NewGuid(), "Einfamilienhaus in Linz-Urfahr", "Hauptstrasse 15", "Linz", 349000, 145, 520, 5, PropertyType.Haus, SellerType.Makler, "Mustermann Immobilien",
                ["https://picsum.photos/seed/haus1a/800/600", "https://picsum.photos/seed/haus1b/800/600", "https://picsum.photos/seed/haus1c/800/600"]),
            new(Guid.NewGuid(), "Modernes Reihenhaus in Wels", "Ringstrasse 42", "Wels", 289000, 120, 180, 4, PropertyType.Haus, SellerType.Privat, "Familie Huber",
                ["https://picsum.photos/seed/haus2a/800/600", "https://picsum.photos/seed/haus2b/800/600"]),
            new(Guid.NewGuid(), "Familienhaus in Steyr", "Bahnhofstrasse 67", "Steyr", 315000, 135, 450, 5, PropertyType.Haus, SellerType.Makler, "Immobilien Steyr",
                ["https://picsum.photos/seed/haus3a/800/600", "https://picsum.photos/seed/haus3b/800/600", "https://picsum.photos/seed/haus3c/800/600", "https://picsum.photos/seed/haus3d/800/600"]),
            new(Guid.NewGuid(), "Baugrundstück in Wels", "Neubaugebiet Sued", "Wels", 189000, null, 850, null, PropertyType.Grundstueck, SellerType.Privat, "Familie Mueller",
                ["https://picsum.photos/seed/grund1a/800/600", "https://picsum.photos/seed/grund1b/800/600"]),
            new(Guid.NewGuid(), "Sonniges Baugrundstück Linz-Land", "Am Sonnenhang 12", "Leonding", 245000, null, 720, null, PropertyType.Grundstueck, SellerType.Makler, "Grund & Boden OOe",
                ["https://picsum.photos/seed/grund2a/800/600", "https://picsum.photos/seed/grund2b/800/600", "https://picsum.photos/seed/grund2c/800/600"]),
            new(Guid.NewGuid(), "Zwangsversteigerung: Haus in Traun", "Industriestrasse 45", "Traun", 185000, 110, 380, 4, PropertyType.Zwangsversteigerung, SellerType.Makler, "Bezirksgericht Linz",
                ["https://picsum.photos/seed/zwang1a/800/600", "https://picsum.photos/seed/zwang1b/800/600"]),
        };

        ApplyFilters();
    }
}
