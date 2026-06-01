using System;
using System.Threading;
using System.Threading.Tasks;

namespace Heimatplatz.Api.Cleanup;

/// <summary>
/// Contributor-Pattern fuer das vollstaendige Loeschen aller Daten eines Benutzers
/// (Account-Loeschung gemaess Apple Guideline 5.1.1(v) / DSGVO).
///
/// Jedes Feature, das benutzerbezogene Daten speichert, implementiert einen eigenen
/// Eraser. Der zentrale DeleteAccountHandler (Auth-Feature) ruft alle registrierten
/// Eraser in aufsteigender <see cref="Order"/>-Reihenfolge innerhalb einer Transaktion auf.
///
/// Dadurch bleibt das Auth-Feature von anderen Features entkoppelt (kein direkter
/// Verweis auf deren Entities) und neue Features koennen ihre Loeschlogik selbst beitragen.
/// </summary>
public interface IUserDataEraser
{
    /// <summary>
    /// Ausfuehrungsreihenfolge (niedriger Wert = zuerst). Wird genutzt, um eine
    /// FK-sichere Loeschreihenfolge ueber Feature-Grenzen hinweg zu garantieren.
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Loescht alle Daten des angegebenen Benutzers, die zu diesem Feature gehoeren.
    /// Implementierungen muessen idempotent sein (kein Fehler, wenn keine Daten existieren).
    /// </summary>
    /// <param name="userId">ID des zu loeschenden Benutzers.</param>
    /// <param name="cancellationToken">Abbruch-Token.</param>
    Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default);
}
