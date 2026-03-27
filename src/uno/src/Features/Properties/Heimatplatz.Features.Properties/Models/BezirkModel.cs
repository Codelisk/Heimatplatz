using CommunityToolkit.Mvvm.ComponentModel;

namespace Heimatplatz.Features.Properties.Models;

/// <summary>
/// Gemeinde innerhalb eines Bezirks
/// </summary>
public partial class GemeindeModel : ObservableObject
{
    public Guid Id { get; }
    public string Name { get; }
    public string PostalCode { get; }

    [ObservableProperty]
    private bool _isSelected;

    public GemeindeModel(Guid id, string name, string postalCode)
    {
        Id = id;
        Name = name;
        PostalCode = postalCode;
    }
}

/// <summary>
/// Bezirk mit untergeordneten Gemeinden (aufklappbar)
/// </summary>
public partial class BezirkModel : ObservableObject
{
    public Guid Id { get; }
    public string Name { get; }

    [ObservableProperty]
    private bool _isExpanded;

    /// <summary>
    /// Ob die "Alle auswählen" Checkbox angezeigt wird (false im Single-Select Modus)
    /// </summary>
    [ObservableProperty]
    private bool _showSelectAll = true;

    public List<GemeindeModel> Gemeinden { get; }

    /// <summary>
    /// Ob alle Gemeinden im Bezirk ausgewaehlt sind (null = teilweise)
    /// </summary>
    public bool? AllGemeindenSelected
    {
        get
        {
            if (Gemeinden.Count == 0) return false;
            var count = Gemeinden.Count(g => g.IsSelected);
            if (count == 0) return false;
            if (count == Gemeinden.Count) return true;
            return null;
        }
    }

    public void NotifyAllGemeindenSelectedChanged() => OnPropertyChanged(nameof(AllGemeindenSelected));

    public BezirkModel(Guid id, string name, List<GemeindeModel> gemeinden)
    {
        Id = id;
        Name = name;
        Gemeinden = gemeinden;
    }
}
