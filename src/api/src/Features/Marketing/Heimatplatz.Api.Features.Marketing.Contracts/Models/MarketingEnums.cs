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
/// Automatische Uebergaenge: Versand setzt Lead/ToContact->Contacted, eingehende Antwort
/// setzt Lead/ToContact/Contacted/FollowUp->Replied; alles Weitere pflegt der Nutzer manuell.
/// </summary>
public enum MarketingContactStatus
{
    Lead = 0,
    Contacted = 1,
    Replied = 2,
    Interested = 3,
    Customer = 4,
    NotInterested = 5,
    DoNotContact = 6,

    /// <summary>Aus dem Firmenpool zur Kontaktaufnahme vorgemerkt - die Arbeitsliste</summary>
    ToContact = 7,

    /// <summary>Kontaktiert, mit vereinbartem Wiedervorlage-Termin (NextFollowUpAt)</summary>
    FollowUp = 8
}

/// <summary>
/// Art eines Eintrags in der Kontakt-Historie. Der Versand von Marketing-Mails erzeugt
/// KEINE Aktivitaet - E-Mails haengen als eigene Entitaet am Kontakt und werden in der
/// Timeline zusammengefuehrt.
/// </summary>
public enum MarketingActivityType
{
    /// <summary>Freie Notiz ohne Kontaktaufnahme</summary>
    Note = 0,

    /// <summary>Telefonat</summary>
    Call = 1,

    /// <summary>Statuswechsel (StatusFrom -> StatusTo)</summary>
    StatusChange = 2,

    /// <summary>Wiedervorlage-Termin gesetzt oder verschoben</summary>
    FollowUp = 3,

    /// <summary>Persoenlicher Termin</summary>
    Meeting = 4
}

/// <summary>Helfer rund um <see cref="MarketingContactStatus"/>.</summary>
public static class MarketingContactStatusExtensions
{
    /// <summary>
    /// Endzustaende des Funnels - hier ist keine weitere Bearbeitung und keine offene
    /// Wiedervorlage mehr zu erwarten. Beim Uebergang in einen dieser Status wird eine
    /// gesetzte Wiedervorlage geleert, damit der Kontakt nicht dauerhaft als "faellig" gilt.
    /// EF kann diese Methode nicht uebersetzen - in Queries die Bedingung inline halten
    /// (Customer/NotInterested/DoNotContact) und hier als einzige Quelle der Wahrheit fuehren.
    /// </summary>
    public static bool IsClosed(this MarketingContactStatus status) =>
        status is MarketingContactStatus.Customer
            or MarketingContactStatus.NotInterested
            or MarketingContactStatus.DoNotContact;
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
