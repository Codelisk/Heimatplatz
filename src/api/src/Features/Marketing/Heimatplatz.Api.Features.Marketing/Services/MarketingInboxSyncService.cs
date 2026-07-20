using System.Text.RegularExpressions;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// IMAP-Abruf via MailKit (gleiche Zugangsdaten wie SMTP, Hetzner-Webhosting-Mailbox).
/// Betrachtet nur Mails der letzten 30 Tage und uebernimmt AUSSCHLIESSLICH:
///  - Antworten auf versendete Marketing-Mails (In-Reply-To/References -> MessageId), oder
///  - Mails von Adressen, die als Kontakt in der Datenbank stehen.
/// Alles andere (allgemeines Postfach, eigene Mails, System-Mails) bleibt unangetastet -
/// info@ ist auch das allgemeine Postfach des Betreibers.
/// Idempotent ueber den Unique-Index auf MarketingInboundEmails.MessageId.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class MarketingInboxSyncService(
    AppDbContext dbContext,
    IOptions<EmailOptions> emailOptions,
    ILogger<MarketingInboxSyncService> logger
) : IMarketingInboxSync
{
    private const int SyncWindowDays = 30;
    private const int MaxBodyLength = 8000;
    private static readonly TimeSpan AutoSyncThrottle = TimeSpan.FromMinutes(5);

    // Prozessweiter Zustand: verhindert parallele IMAP-Sessions und drosselt den
    // Auto-Sync der Posteingang-Seite (Service selbst ist scoped).
    private static readonly SemaphoreSlim SyncLock = new(1, 1);
    private static DateTimeOffset lastSyncAt = DateTimeOffset.MinValue;

    public bool IsConfigured => emailOptions.Value.IsImapConfigured && emailOptions.Value.IsConfigured;

    public async Task<MarketingInboxSyncResult> SyncAsync(bool force, CancellationToken ct = default)
    {
        if (!IsConfigured)
            return new MarketingInboxSyncResult(false, Skipped: true, 0,
                "E-Mail-Postfach nicht konfiguriert (EMAIL_SMTP_* fehlt) – Abruf nicht möglich.");

        if (!force && DateTimeOffset.UtcNow - lastSyncAt < AutoSyncThrottle)
            return new MarketingInboxSyncResult(true, Skipped: true, 0, null);

        if (!await SyncLock.WaitAsync(TimeSpan.Zero, ct))
            return new MarketingInboxSyncResult(true, Skipped: true, 0, null);

        try
        {
            var added = await RunSyncAsync(ct);
            lastSyncAt = DateTimeOffset.UtcNow;
            return new MarketingInboxSyncResult(true, Skipped: false, added, null);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogError(ex, "[Marketing] Posteingang-Sync fehlgeschlagen");
            return new MarketingInboxSyncResult(false, Skipped: false, 0, Truncate(ex.Message, 300));
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<int> RunSyncAsync(CancellationToken ct)
    {
        var opts = emailOptions.Value;
        var since = DateTimeOffset.UtcNow.AddDays(-SyncWindowDays);

        // Zuordnungs-Daten vorab laden: Message-Ids versendeter Mails + bekannte Adressen
        var sentByMessageId = await dbContext.Set<MarketingEmail>()
            .Where(e => e.MessageId != null)
            .Select(e => new { MessageId = e.MessageId!, e.Id, e.ContactId })
            .ToDictionaryAsync(e => e.MessageId, e => (e.Id, e.ContactId), StringComparer.OrdinalIgnoreCase, ct);

        var contactsByEmail = await dbContext.Set<MarketingContact>()
            .Select(c => new { c.Email, c.Id })
            .ToDictionaryAsync(c => c.Email, c => c.Id, StringComparer.OrdinalIgnoreCase, ct);

        var knownMessageIds = new HashSet<string>(
            await dbContext.Set<MarketingInboundEmail>().Select(i => i.MessageId).ToListAsync(ct),
            StringComparer.OrdinalIgnoreCase);

        var ownAddress = opts.FromAddress;

        using var client = new ImapClient();
        client.Timeout = 30_000;

        var socketOptions = opts.ImapPort == 993
            ? SecureSocketOptions.SslOnConnect
            : SecureSocketOptions.StartTls;
        await client.ConnectAsync(opts.EffectiveImapHost, opts.ImapPort, socketOptions, ct);
        await client.AuthenticateAsync(opts.SmtpUsername, opts.SmtpPassword, ct);

        var inbox = client.Inbox;
        await inbox.OpenAsync(FolderAccess.ReadOnly, ct);

        // IMAP-SINCE arbeitet nur datumsgenau - die Message-Id-Pruefung dedupliziert
        var uids = await inbox.SearchAsync(SearchQuery.DeliveredAfter(since.UtcDateTime.Date), ct);
        if (uids.Count == 0)
        {
            await client.DisconnectAsync(true, ct);
            return 0;
        }

        var summaries = await inbox.FetchAsync(uids,
            MessageSummaryItems.UniqueId | MessageSummaryItems.Envelope | MessageSummaryItems.References, ct);

        var added = 0;
        var updatedContacts = new Dictionary<Guid, MarketingContact>();

        foreach (var summary in summaries)
        {
            ct.ThrowIfCancellationRequested();

            var envelope = summary.Envelope;
            if (envelope is null)
                continue;

            var from = envelope.From?.Mailboxes?.FirstOrDefault()?.Address;
            if (string.IsNullOrWhiteSpace(from))
                continue;

            // Eigene Mails (z.B. Selbst-Tests, BCC-Kopien) nicht als Rueckmeldung werten
            if (string.Equals(from, ownAddress, StringComparison.OrdinalIgnoreCase))
                continue;

            // Idempotenz: Fallback-Id fuer (seltene) Mails ohne Message-Id-Header
            var messageId = NormalizeMessageId(envelope.MessageId)
                ?? $"uid:{inbox.UidValidity}:{summary.UniqueId.Id}";
            if (knownMessageIds.Contains(messageId))
                continue;

            // Zuordnung 1: Antwort auf eine versendete Marketing-Mail?
            Guid? repliedToEmailId = null;
            Guid? contactId = null;
            var referencedIds = (summary.References ?? Enumerable.Empty<string>())
                .Append(envelope.InReplyTo)
                .Select(NormalizeMessageId)
                .Where(id => id != null);
            foreach (var refId in referencedIds)
            {
                if (sentByMessageId.TryGetValue(refId!, out var sent))
                {
                    repliedToEmailId = sent.Id;
                    contactId = sent.ContactId;
                    break;
                }
            }

            // Zuordnung 2: Absender ist ein bekannter Kontakt?
            if (contactId is null && contactsByEmail.TryGetValue(from.Trim(), out var byAddress))
                contactId = byAddress;

            // Weder Antwort noch bekannter Kontakt -> gehoert nicht in den Marketing-Eingang
            if (contactId is null)
                continue;

            var mime = await inbox.GetMessageAsync(summary.UniqueId, ct);
            var bodyText = mime.TextBody;
            if (string.IsNullOrWhiteSpace(bodyText) && !string.IsNullOrWhiteSpace(mime.HtmlBody))
                bodyText = HtmlToPlainText(mime.HtmlBody);

            var receivedAt = envelope.Date ?? DateTimeOffset.UtcNow;

            dbContext.Set<MarketingInboundEmail>().Add(new MarketingInboundEmail
            {
                Id = Guid.NewGuid(),
                ContactId = contactId,
                MarketingEmailId = repliedToEmailId,
                FromAddress = from.Trim(),
                FromName = envelope.From?.Mailboxes?.FirstOrDefault()?.Name,
                Subject = Truncate(envelope.Subject ?? "", 500),
                BodyText = Truncate(bodyText?.Trim() ?? "", MaxBodyLength),
                MessageId = messageId,
                InReplyTo = NormalizeMessageId(envelope.InReplyTo),
                ReceivedAt = receivedAt,
                IsRead = false
            });
            knownMessageIds.Add(messageId);
            added++;

            // Kontakt-Status nachziehen (nur automatische Uebergaenge, nichts ueberschreiben,
            // was der Nutzer manuell weitergeschaltet hat)
            if (!updatedContacts.TryGetValue(contactId.Value, out var contact))
            {
                contact = await dbContext.Set<MarketingContact>().FirstOrDefaultAsync(c => c.Id == contactId, ct);
                if (contact is null)
                    continue;
                updatedContacts[contactId.Value] = contact;
            }

            if (contact.LastReplyAt is null || contact.LastReplyAt < receivedAt)
                contact.LastReplyAt = receivedAt;
            if (contact.Status is MarketingContactStatus.Lead or MarketingContactStatus.Contacted)
                contact.Status = MarketingContactStatus.Replied;
        }

        await client.DisconnectAsync(true, ct);

        if (added > 0)
            await dbContext.SaveChangesAsync(ct);

        logger.LogInformation("[Marketing] Posteingang-Sync abgeschlossen: {Added} neue Rückmeldung(en) aus {Total} geprüften Mails",
            added, summaries.Count);
        return added;
    }

    /// <summary>Message-Ids vergleichbar machen: spitze Klammern/Whitespace entfernen.</summary>
    private static string? NormalizeMessageId(string? messageId)
    {
        var trimmed = messageId?.Trim().TrimStart('<').TrimEnd('>');
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    /// <summary>Grobe Text-Extraktion fuer HTML-only-Mails (reicht fuer die Vorschau).</summary>
    private static string HtmlToPlainText(string html)
    {
        var withoutBlocks = Regex.Replace(html, "<(script|style)[^>]*>.*?</\\1>", " ",
            RegexOptions.Singleline | RegexOptions.IgnoreCase);
        var withBreaks = Regex.Replace(withoutBlocks, "<(br|/p|/div)[^>]*>", "\n", RegexOptions.IgnoreCase);
        var withoutTags = Regex.Replace(withBreaks, "<[^>]+>", " ");
        var decoded = System.Net.WebUtility.HtmlDecode(withoutTags);
        return Regex.Replace(decoded, "[ \\t]{2,}", " ").Trim();
    }

    private static string Truncate(string value, int maxLength) =>
        value.Length <= maxLength ? value : value[..maxLength] + "…";
}
