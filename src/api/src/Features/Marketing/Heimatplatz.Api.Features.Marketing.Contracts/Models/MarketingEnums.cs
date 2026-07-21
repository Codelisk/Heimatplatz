namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// Art des Kontakts in der Marketing-Kontaktdatenbank. Serialisiert per globalem
/// JsonStringEnumConverter als Text ("Broker" statt 1) - Web vergleicht Strings.
/// </summary>
public enum MarketingContactType
{
    Unknown = 0,
    Broker = 1,
    PropertyManager = 2,
    PrivateSeller = 3,
    Municipality = 4,
    Partner = 5,
    Other = 6
}

/// <summary>
/// Bearbeitungsstatus eines Kontakts im Marketing-Funnel.
/// Automatische Uebergaenge: Versand setzt Lead->Contacted, eingehende Antwort setzt
/// Lead/Contacted->Replied; alles Weitere pflegt der Nutzer manuell.
/// </summary>
public enum MarketingContactStatus
{
    Lead = 0,
    Contacted = 1,
    Replied = 2,
    Interested = 3,
    Customer = 4,
    NotInterested = 5,
    DoNotContact = 6
}

/// <summary>Versand-Ergebnis einer Marketing-E-Mail.</summary>
public enum MarketingEmailStatus
{
    /// <summary>Per SMTP versendet (vom Mailserver angenommen)</summary>
    Sent = 0,

    /// <summary>Kein SMTP konfiguriert - nur im Log ausgegeben, nicht zugestellt</summary>
    LoggedOnly = 1,

    /// <summary>Unzustellbar: der Posteingang-Sync hat einen Bounce/NDR zugeordnet</summary>
    DeliveryFailed = 2
}
