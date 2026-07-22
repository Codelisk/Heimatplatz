namespace Heimatplatz.Maui.Core.Theming;

/// <summary>
/// Verwaltet den Design-Modus der App (System/Hell/Dunkel): persistiert die Wahl
/// geraetegebunden und wendet sie auf <see cref="Application.UserAppTheme"/> an,
/// damit alle AppThemeBindings sofort umschalten.
/// </summary>
public interface IThemeService
{
    /// <summary>Aktuell gewaehlter Modus.</summary>
    AppThemeMode Mode { get; }

    /// <summary>Liest die gespeicherte Wahl und wendet sie an (einmalig beim App-Start).</summary>
    void Initialize();

    /// <summary>Schaltet zum naechsten Modus im Zyklus System -&gt; Hell -&gt; Dunkel und wendet ihn an.</summary>
    AppThemeMode CycleMode();

    /// <summary>Wendet den aktuellen Modus erneut an (z.B. sobald das Window steht).</summary>
    void Apply();

    /// <summary>
    /// Wendet den aktuellen Modus auf das bereits von der Plattform erzeugte native
    /// Window an, bevor der visuelle Baum aufgebaut wird.
    /// </summary>
    void PrepareWindow(IActivationState? activationState);
}
