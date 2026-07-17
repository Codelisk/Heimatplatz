using CommunityToolkit.Mvvm.ComponentModel;

namespace Heimatplatz.Maui;

/// <summary>
/// Ein Eintrag der selbstgebauten Flyout-Liste (siehe AppShell.FlyoutContent).
/// Roots zeigen auf Shell-Root-Routen (absolute Navigation), Aktionen wie
/// "Immobilie hinzufuegen" pushen ihre Seite relativ (Zurueck-Pfeil zum Abbrechen).
/// </summary>
public partial class FlyoutMenuEntry : ObservableObject
{
    public required string Title { get; init; }

    public required string Route { get; init; }

    public ImageSource? Icon { get; init; }

    /// <summary>True = Shell-Root (Tab-Wechsel), false = Aktion (Push)</summary>
    public bool IsRoot { get; init; } = true;

    /// <summary>Fuer UI-Automatisierung (DevFlow) adressierbar</summary>
    public string AutomationId => $"Flyout_{Route}";

    [ObservableProperty]
    public partial bool IsSelected { get; set; }
}
