using Heimatplatz.Api.Features.Properties.Contracts.Enums;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <summary>
/// Serverseitig abgeleitete Anbieter-Informationen fuer ein Inserat
/// </summary>
/// <param name="SellerType">Anbietertyp aus dem Benutzerprofil</param>
/// <param name="SellerName">Anzeigename (Firmenname bei Broker/Verwaltung, sonst voller Name)</param>
/// <param name="SellerSourceId">FK zur SellerSource (nur Broker/Verwaltung)</param>
/// <param name="ContactType">Kontaktart fuer den automatisch angehaengten Kontakt</param>
public record SellerInfo(
    SellerType SellerType,
    string SellerName,
    Guid? SellerSourceId,
    ContactType ContactType
);

/// <summary>
/// Leitet die Anbieter-Daten eines Inserats aus dem Profil des Eigentuemers ab (Backend-First:
/// der Client hat auf SellerType/SellerName keinen Einfluss) und pflegt die SellerSource-Tabelle.
/// </summary>
public interface ISellerInfoResolver
{
    /// <summary>
    /// Ermittelt SellerType, SellerName, SellerSource-FK und ContactType fuer den angegebenen
    /// Benutzer. Wirft ValidationException, wenn der Benutzer kein Verkaeufer ist.
    /// </summary>
    Task<SellerInfo> ResolveForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Findet die SellerSource mit dem angegebenen Namen oder legt sie an (case-insensitiv).
    /// Fuer Private-Anbieter (kein Firmenname) null zurueckgeben - nur Broker/Verwaltungen
    /// erscheinen im Anbieter-Ausschlussfilter.
    /// </summary>
    Task<Guid?> ResolveSellerSourceAsync(SellerType sellerType, string sellerName, CancellationToken cancellationToken = default);
}
