namespace Heimatplatz.Maui.Features.Properties.Controls;

/// <summary>
/// Vollbild-Bildviewer der Detailseiten. Bewusst ein eigenes Control: die Seiten
/// erzeugen es erst, wenn der Benutzer ein Foto tatsaechlich gross ansieht - vorher
/// kostet es weder XAML-Aufbau noch Layout-Durchlaeufe bei jeder Navigation.
/// </summary>
public partial class PropertyImageViewerOverlay : ContentView
{
    /// <param name="automationPrefix">
    /// Praefix der AutomationIds ("Detail" bzw. "Foreclosure"), damit die Buttons je
    /// Seite eindeutig ansprechbar bleiben (DevFlow).
    /// </param>
    public PropertyImageViewerOverlay(string automationPrefix)
    {
        InitializeComponent();

        PreviousButton.AutomationId = $"{automationPrefix}_Viewer_Previous";
        NextButton.AutomationId = $"{automationPrefix}_Viewer_Next";
        CloseButton.AutomationId = $"{automationPrefix}_Viewer_Close";
    }
}
