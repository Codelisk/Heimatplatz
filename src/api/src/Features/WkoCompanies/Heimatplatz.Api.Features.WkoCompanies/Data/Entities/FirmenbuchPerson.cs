namespace Heimatplatz.Api.Features.WkoCompanies.Data.Entities;

/// <summary>
/// Person aus dem amtlichen Firmenbuch-Auszug (Geschaeftsfuehrung o.ae.), ueber die
/// FBW-WebServices (HVD)-Schnittstelle angereichert. Wird wie WkoCompanyPermit als
/// JSON-Liste auf der Entity gespeichert - kein eigenes Kind-Entity noetig.
/// </summary>
public record FirmenbuchPerson
{
    public string? Name { get; init; }
    public DateOnly? BirthDate { get; init; }

    /// <summary>Funktionsbezeichnung laut Firmenbuch (z.B. "GESCHÄFTSFÜHRER/IN (handelsrechtlich)")</summary>
    public string? Role { get; init; }
}
