namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Domain-Fallback der Posteingang-Zuordnung: Wenn weder Reply-Header noch die
/// Absender-Adresse einen Kontakt treffen, entscheidet die Absender-Domain - sofern sie
/// eindeutig zu genau EINEM Kontakt gehoert und kein oeffentlicher Mail-Provider ist.
/// Typischer Fall: office@firma.at ist als Kontakt hinterlegt, der Ansprechpartner
/// antwortet aber von vorname.nachname@firma.at (z.B. als Weiterleitung ohne Reply-Header).
/// Reine Logik ohne Abhaengigkeiten - unit-getestet in MarketingInboundMatchingTests.
/// </summary>
public static class MarketingInboundMatching
{
    /// <summary>Domain-Teil einer Adresse, lowercase; null wenn keine erkennbar ist.</summary>
    public static string? ExtractDomain(string? emailAddress)
    {
        var at = emailAddress?.LastIndexOf('@') ?? -1;
        if (emailAddress is null || at < 0 || at == emailAddress.Length - 1)
            return null;

        var domain = emailAddress[(at + 1)..].Trim().ToLowerInvariant();
        return domain.Length == 0 || !domain.Contains('.') ? null : domain;
    }

    /// <summary>
    /// Index Domain -> Kontakt aus allen bekannten Kontakt-Adressen (Versand- und
    /// Zusatzadressen). Domains oeffentlicher Provider fliegen komplett raus; teilen sich
    /// mehrere Kontakte eine Domain, steht null im Index (mehrdeutig - nie raten).
    /// </summary>
    public static Dictionary<string, Guid?> BuildDomainIndex(
        IEnumerable<KeyValuePair<string, Guid>> contactAddresses,
        IEnumerable<string> publicEmailDomains)
    {
        var publicDomains = new HashSet<string>(
            publicEmailDomains.Select(d => d.Trim().ToLowerInvariant()),
            StringComparer.Ordinal);

        var index = new Dictionary<string, Guid?>(StringComparer.Ordinal);
        foreach (var (address, contactId) in contactAddresses)
        {
            var domain = ExtractDomain(address);
            if (domain is null || publicDomains.Contains(domain))
                continue;

            if (index.TryGetValue(domain, out var existing))
            {
                if (existing != contactId)
                    index[domain] = null;
            }
            else
            {
                index[domain] = contactId;
            }
        }

        return index;
    }

    /// <summary>Absender-Domain im Index nachschlagen; nur eindeutige Treffer zaehlen.</summary>
    public static bool TryResolveByDomain(
        string fromAddress,
        IReadOnlyDictionary<string, Guid?> domainIndex,
        out Guid contactId)
    {
        contactId = default;
        var domain = ExtractDomain(fromAddress);
        if (domain is null || !domainIndex.TryGetValue(domain, out var match) || match is null)
            return false;

        contactId = match.Value;
        return true;
    }
}
