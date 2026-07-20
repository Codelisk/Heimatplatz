using Heimatplatz.Api.Core.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;

namespace Heimatplatz.Api.Features.Marketing.Data.Entities;

/// <summary>
/// Versendete Marketing-E-Mail (Versand-Historie). Der Body ist der Fliesstext OHNE
/// Signatur (die haengt der Composer beim Versand an). MessageId ist die SMTP-Message-Id -
/// eingehende Antworten werden darueber (In-Reply-To/References) zugeordnet.
/// </summary>
public class MarketingEmail : BaseEntity
{
    public Guid ContactId { get; set; }
    public MarketingContact Contact { get; set; } = null!;

    public required string Subject { get; set; }

    public required string Body { get; set; }

    /// <summary>Stichwoerter der KI-Generierung (fuer spaetere Auswertung, was funktioniert)</summary>
    public string? Keywords { get; set; }

    /// <summary>SMTP-Message-Id ohne spitze Klammern (Reply-Threading)</summary>
    public string? MessageId { get; set; }

    public MarketingEmailStatus Status { get; set; } = MarketingEmailStatus.Sent;

    public DateTimeOffset SentAt { get; set; }

    public ICollection<MarketingInboundEmail> Replies { get; set; } = [];
}
