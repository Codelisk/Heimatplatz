namespace Heimatplatz.Api.Features.Firmenbuch.Data.Entities;

/// <summary>
/// Person aus dem amtlichen Firmenbuch-Auszug (Geschaeftsfuehrung o.ae.), ueber die
/// FBW-WebServices (HVD)-Schnittstelle geladen.
/// </summary>
public record FirmenbuchPerson
{
    public string? Name { get; init; }
    public DateOnly? BirthDate { get; init; }

    /// <summary>Funktionsbezeichnung laut Firmenbuch (z.B. "GESCHÄFTSFÜHRER/IN (handelsrechtlich)")</summary>
    public string? Role { get; init; }
}
