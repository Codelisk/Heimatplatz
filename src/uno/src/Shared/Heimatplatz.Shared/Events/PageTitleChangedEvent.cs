using Shiny.Mediator;

namespace Heimatplatz.Events;

/// <summary>
/// Event das bei Aenderung des Page-Headers publiziert wird.
/// Enthaelt Titel und optionalen HeaderContent (z.B. Action Buttons).
/// </summary>
/// <param name="Title">Der anzuzeigende Titel (null/leer = "HEIMATPLATZ")</param>
/// <param name="HeaderContent">Optionaler Content fuer den Header (z.B. StackPanel mit Buttons)</param>
public record PageHeaderChangedEvent(string? Title, object? HeaderContent = null) : IEvent;
