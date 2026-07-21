using Heimatplatz.Api.Core.Data.Entities;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Eingegangene Rueckmeldung aus dem Postfach info@heimatplatz.at. Der Posteingang-Sync
/// (MarketingInboxSyncService) uebernimmt NUR Mails, die entweder eine Antwort auf eine
/// versendete Marketing-Mail sind (In-Reply-To/References) oder von einer bekannten
/// Kontakt-Adresse stammen - das restliche Postfach bleibt bewusst aussen vor.
/// </summary>
public class MarketingInboundEmail : BaseEntity
{
    /// <summary>Zugeordneter Kontakt (Absender-Adresse); null nur wenn Kontakt geloescht wurde</summary>
    public Guid? ContactId { get; set; }
    public MarketingContact? Contact { get; set; }

    /// <summary>Die Marketing-Mail, auf die geantwortet wurde (via In-Reply-To/References)</summary>
    public Guid? MarketingEmailId { get; set; }
    public MarketingEmail? RepliedToEmail { get; set; }

    public required string FromAddress { get; set; }

    public string? FromName { get; set; }

    public string? Subject { get; set; }

    /// <summary>Text-Part der Mail, gekuerzt (max. 8000 Zeichen)</summary>
    public string? BodyText { get; set; }

    /// <summary>Message-Id der eingehenden Mail (Idempotenz-Schluessel des Syncs)</summary>
    public required string MessageId { get; set; }

    public string? InReplyTo { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public bool IsRead { get; set; }

    /// <summary>
    /// Unzustellbarkeits-Meldung (Bounce/NDR, z.B. von mailer-daemon) statt echter
    /// Antwort. Bounces zaehlen nicht als Rueckmeldung des Kontakts und setzen den
    /// Kontakt-Status NICHT auf Replied; die referenzierte Versand-Mail wird
    /// stattdessen als DeliveryFailed markiert.
    /// </summary>
    public bool IsBounce { get; set; }
}
