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

    public List<GemeindeModel> Gemeinden { get; }

    public BezirkModel(Guid id, string name, List<GemeindeModel> gemeinden)
    {
        Id = id;
        Name = name;
        Gemeinden = gemeinden;
    }
}
