using CommunityToolkit.Mvvm.ComponentModel;
using Heimatplatz.Features.Properties.Contracts.Models;

namespace Heimatplatz.Features.Properties.Models;

/// <summary>
/// Model fuer einen Anbietertyp mit Auswahl-Status
/// </summary>
public partial class SellerTypeModel : ObservableObject
{
    public SellerType Type { get; }
    public string DisplayName { get; }

    [ObservableProperty]
    private bool _isSelected = true;

    public SellerTypeModel(SellerType type, string displayName, bool isSelected = true)
    {
        Type = type;
        DisplayName = displayName;
        _isSelected = isSelected;
    }

    /// <summary>
    /// Erstellt die Standard-Liste aller Anbietertypen
    /// </summary>
    public static List<SellerTypeModel> CreateDefaultList(
        bool isPrivateSelected = true,
        bool isBrokerSelected = true,
        bool isPortalSelected = true)
    {
        return
        [
            new SellerTypeModel(SellerType.Private, "Privat", isPrivateSelected),
            new SellerTypeModel(SellerType.Broker, "Makler", isBrokerSelected),
            new SellerTypeModel(SellerType.Portal, "Portal", isPortalSelected)
        ];
    }
}
