using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Welche Sammlung des Benutzers eine Statusaenderung betrifft.
/// </summary>
public enum PropertyStatusKind
{
    Favorite,
    Blocked
}

/// <summary>
/// Wird publiziert, wenn eine Immobilie zu den Favoriten bzw. Blockierten hinzukommt
/// oder daraus entfernt wird - egal von welcher Seite aus. Die Sammlungsseiten ziehen
/// ihre bereits geladene Liste damit nach; das C#-Event
/// <see cref="IPropertyStatusService.StatusChanged"/> bleibt fuer die Herz-/Blockier-
/// Symbole auf den Karten zustaendig.
/// </summary>
/// <param name="PropertyId">Betroffene Immobilie</param>
/// <param name="Kind">Favoriten- oder Blockiert-Sammlung</param>
/// <param name="IsMember">True wenn die Immobilie jetzt in der Sammlung ist, false wenn entfernt</param>
public record PropertyStatusChangedEvent(
    Guid PropertyId,
    PropertyStatusKind Kind,
    bool IsMember
) : IEvent;
