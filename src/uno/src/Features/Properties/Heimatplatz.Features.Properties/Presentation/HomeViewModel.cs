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

    public Visibility IsEmpty => Properties.Count == 0 ? Visibility.Visible : Visibility.Collapsed;

    public HomeViewModel()
    {
        // TODO: API-Integration - vorerst Testdaten laden
        LoadTestData();
    }

    partial void OnIsAllSelectedChanged(bool value)
    {
        if (value)
        {
            IsHausSelected = false;
            IsGrundstueckSelected = false;
            IsZwangsversteigerungSelected = false;
            LoadTestData();
        }
    }

    partial void OnIsHausSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
            FilterProperties(PropertyType.Haus);
        }
    }

    partial void OnIsGrundstueckSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
            FilterProperties(PropertyType.Grundstueck);
        }
    }

    partial void OnIsZwangsversteigerungSelectedChanged(bool value)
    {
        if (value)
        {
            IsAllSelected = false;
            FilterProperties(PropertyType.Zwangsversteigerung);
        }
    }

    private void FilterProperties(PropertyType typ)
    {
        var filtered = _allProperties.Where(p => p.Typ == typ).ToList();
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
            new(Guid.NewGuid(), "Einfamilienhaus in Linz-Urfahr", "Hauptstrasse 15", "Linz", 349000, 145, 520, 5, PropertyType.Haus, SellerType.Makler, "Mustermann Immobilien", "https://picsum.photos/seed/haus1/800/600"),
            new(Guid.NewGuid(), "Modernes Reihenhaus in Wels", "Ringstrasse 42", "Wels", 289000, 120, 180, 4, PropertyType.Haus, SellerType.Privat, "Familie Huber", "https://picsum.photos/seed/haus2/800/600"),
            new(Guid.NewGuid(), "Familienhaus in Steyr", "Bahnhofstrasse 67", "Steyr", 315000, 135, 450, 5, PropertyType.Haus, SellerType.Makler, "Immobilien Steyr", "https://picsum.photos/seed/haus3/800/600"),
            new(Guid.NewGuid(), "Baugrundstück in Wels", "Neubaugebiet Sued", "Wels", 189000, null, 850, null, PropertyType.Grundstueck, SellerType.Privat, "Familie Mueller", "https://picsum.photos/seed/grund1/800/600"),
            new(Guid.NewGuid(), "Sonniges Baugrundstück Linz-Land", "Am Sonnenhang 12", "Leonding", 245000, null, 720, null, PropertyType.Grundstueck, SellerType.Makler, "Grund & Boden OOe", "https://picsum.photos/seed/grund2/800/600"),
            new(Guid.NewGuid(), "Zwangsversteigerung: Haus in Traun", "Industriestrasse 45", "Traun", 185000, 110, 380, 4, PropertyType.Zwangsversteigerung, SellerType.Makler, "Bezirksgericht Linz", "https://picsum.photos/seed/zwang1/800/600"),
        };

        Properties.Clear();
        foreach (var property in _allProperties)
        {
            Properties.Add(property);
        }
        OnPropertyChanged(nameof(IsEmpty));
    }
}
