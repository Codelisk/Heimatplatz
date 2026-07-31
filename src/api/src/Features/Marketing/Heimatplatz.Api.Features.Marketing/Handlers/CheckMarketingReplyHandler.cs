using System.Text;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Heimatplatz.Api.Features.Marketing.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// KI-Pruefung eines Antwort-Entwurfs gegen den Gespraechsverlauf des Kontakts
/// (Kontext-Passung, Rechtschreibung/Grammatik, Formulierungsvorschlag). Versendet
/// und speichert nichts - reine Beratung vor dem eigentlichen /inbox/reply.
/// X-Admin-Key-Schutz wie alle /api/admin-Endpoints; Fehler kommen als
/// Success=false + Error, damit der Intern-Bereich die Ursache anzeigen kann.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/marketing")]
public class CheckMarketingReplyHandler(
    IAdminAccessGuard accessGuard,
    IMarketingReplyChecker checker,
    AppDbContext dbContext,
    ILogger<CheckMarketingReplyHandler> logger
) : IRequestHandler<CheckMarketingReplyRequest, CheckMarketingReplyResponse>
{
    /// <summary>Juengste Eintraege, die als Pruefkontext mitgehen (Prompt-Budget)</summary>
    private const int MaxConversationEntries = 10;

    /// <summary>Zeichen-Obergrenze pro Eintrag - lange Mails werden gekuerzt</summary>
    private const int MaxEntryLength = 2000;

    [MediatorHttpPost("/inbox/reply-check", OperationId = "CheckAdminMarketingReply")]
    public async Task<CheckMarketingReplyResponse> Handle(CheckMarketingReplyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        if (string.IsNullOrWhiteSpace(request.Draft))
            return Failed("Der Entwurf darf nicht leer sein.");

        var inbound = await dbContext.Set<MarketingInboundEmail>()
            .Include(i => i.Contact)
            .FirstOrDefaultAsync(i => i.Id == request.InboundEmailId, cancellationToken);
        if (inbound is null)
            return Failed("Die Rückmeldung wurde nicht gefunden.");

        try
        {
            var conversation = await BuildConversationAsync(inbound, cancellationToken);
            var check = await checker.CheckAsync(
                conversation, request.Draft, request.Instruction, request.PreviousSuggestion, cancellationToken);

            return new CheckMarketingReplyResponse(
                true, check.FitsContext, check.ContextNote, check.CorrectedText, check.SuggestedText, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fehlertext bewusst durchreichen: Aufrufer ist ausschliesslich der
            // Admin-Key-authentifizierte Astro-SSR-Server des Intern-Bereichs.
            logger.LogError(ex, "[Marketing] Entwurfsprüfung für Rückmeldung {InboundId} fehlgeschlagen", request.InboundEmailId);
            return Failed(ex.Message);
        }
    }

    /// <summary>
    /// Klartext-Verlauf fuer den Pruef-Prompt: Versand + Rueckmeldungen des Kontakts
    /// (ohne Bounces und ohne Aktivitaeten - Anrufe/Notizen sieht der Kontakt nicht,
    /// fuer die Frage "passt die Antwort zum Schriftverkehr?" zaehlt nur der).
    /// Haengt die Eingangs-Mail ohne Kontakt (geloescht), besteht der Verlauf nur
    /// aus ihr selbst.
    /// </summary>
    private async Task<string> BuildConversationAsync(MarketingInboundEmail inbound, CancellationToken cancellationToken)
    {
        var contactName = inbound.Contact?.Name ?? inbound.FromName ?? inbound.FromAddress;

        var entries = new List<(DateTimeOffset Date, string Author, string? Subject, string Body)>();
        if (inbound.ContactId is Guid contactId)
        {
            var sent = await dbContext.Set<MarketingEmail>()
                .Where(e => e.ContactId == contactId)
                .OrderByDescending(e => e.SentAt)
                .Take(MaxConversationEntries)
                .Select(e => new { e.SentAt, e.Subject, e.Body })
                .ToListAsync(cancellationToken);
            entries.AddRange(sent.Select(e => (e.SentAt, "Heimatplatz", (string?)e.Subject, e.Body)));

            var replies = await dbContext.Set<MarketingInboundEmail>()
                .Where(i => i.ContactId == contactId && !i.IsBounce)
                .OrderByDescending(i => i.ReceivedAt)
                .Take(MaxConversationEntries)
                .Select(i => new { i.ReceivedAt, i.Subject, i.BodyText })
                .ToListAsync(cancellationToken);
            entries.AddRange(replies.Select(i => (i.ReceivedAt, contactName, i.Subject, i.BodyText ?? "")));
        }
        else
        {
            entries.Add((inbound.ReceivedAt, contactName, inbound.Subject, inbound.BodyText ?? ""));
        }

        var sb = new StringBuilder();
        foreach (var entry in entries.OrderBy(e => e.Date).TakeLast(MaxConversationEntries))
        {
            sb.AppendLine($"--- {entry.Author} am {entry.Date:dd.MM.yyyy HH:mm} ---");
            if (!string.IsNullOrWhiteSpace(entry.Subject))
                sb.AppendLine($"Betreff: {entry.Subject}");
            sb.AppendLine(Truncate(entry.Body.Trim(), MaxEntryLength));
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static CheckMarketingReplyResponse Failed(string error) =>
        new(false, false, null, null, null, error);

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
