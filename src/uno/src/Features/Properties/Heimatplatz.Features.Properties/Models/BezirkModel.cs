using CommunityToolkit.Mvvm.ComponentModel;

namespace Heimatplatz.Features.Properties.Models;

/// <summary>
/// Ort innerhalb eines Bezirks
/// </summary>
public partial class OrtModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    private bool _isSelected;

    public OrtModel(string name)
    {
        Name = name;
    }
}

/// <summary>
/// Bezirk mit untergeordneten Orten
/// </summary>
public partial class BezirkModel : ObservableObject
{
    public string Name { get; }

    [ObservableProperty]
    private bool _isExpanded;

    public List<OrtModel> Orte { get; }

    public BezirkModel(string name, params string[] orte)
    {
        Name = name;
        Orte = orte.Select(o => new OrtModel(o)).ToList();
    }
}
