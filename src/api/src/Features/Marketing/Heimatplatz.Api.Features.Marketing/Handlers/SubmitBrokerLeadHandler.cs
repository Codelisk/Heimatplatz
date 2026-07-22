using System.Net.Mail;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Marketing.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Marketing.Contracts.Models;
using Heimatplatz.Api.Features.Marketing.Data.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Handlers;

/// <summary>
/// Oeffentliche Makler-Anfrage der /makler/-Seite (anonym, KEIN Admin-Guard - der
/// Endpoint liegt deshalb bewusst unter /api/marketing statt /api/admin/marketing,
/// das per Caddy-IP-Sperre geschuetzt ist). Upsert in die Marketing-Kontaktdatenbank:
/// neue Adresse wird als Broker/Interested mit Quelle "Makler-Anfrage" angelegt,
/// bekannte Kontakte bekommen die Anfrage als Notiz angehaengt (gepflegte Felder
/// werden nie ueberschrieben). Danach Benachrichtigungs-Mail ans Team-Postfach -
/// deren Fehlschlag ist nicht fatal, der Kontakt ist bereits gespeichert.
/// Spam-Schutz: Honeypot-Feld (still verwerfen) + enges Rate-Limit in Program.cs.
/// </summary>
[AllowAnonymous]
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/marketing")]
public class SubmitBrokerLeadHandler(
    AppDbContext dbContext,
    IEmailSender emailSender,
    IOptions<EmailOptions> emailOptions,
    ILogger<SubmitBrokerLeadHandler> logger
) : IRequestHandler<SubmitBrokerLeadRequest, SubmitBrokerLeadResponse>
{
    // Notes hat DB-seitig max. 8000 Zeichen - beim Anhaengen bleibt das Neueste erhalten
    private const int MaxNotesLength = 8000;

    [MediatorHttpPost("/broker-lead", OperationId = "SubmitBrokerLead")]
    public async Task<SubmitBrokerLeadResponse> Handle(SubmitBrokerLeadRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // Honeypot ausgefuellt: nur Bots sehen dieses Feld - still "Erfolg" melden
        if (!string.IsNullOrWhiteSpace(request.Fax))
            return new SubmitBrokerLeadResponse(true, null);

        var company = Truncate(NullIfEmpty(request.Company), 200);
        if (company is null)
            return new SubmitBrokerLeadResponse(false, "Bitte geben Sie Ihren Firmennamen an.");

        if (!MailAddress.TryCreate(request.Email?.Trim(), out var address))
            return new SubmitBrokerLeadResponse(false, "Die E-Mail-Adresse ist ungültig.");

        var normalizedEmail = address.Address.Trim().ToLowerInvariant();
        var contactName = Truncate(NullIfEmpty(request.ContactName), 200);
        var phone = Truncate(NullIfEmpty(request.Phone), 50);
        var listingsUrl = Truncate(NullIfEmpty(request.ListingsUrl), 500);
        var message = Truncate(NullIfEmpty(request.Message), 2000);

        var now = DateTimeOffset.UtcNow;
        var noteLines = new List<string> { $"[{now:yyyy-MM-dd HH:mm} UTC] Makler-Anfrage über /makler/ (Einrichtung + Inserate übernehmen)" };
        if (listingsUrl is not null)
            noteLines.Add($"Inserate: {listingsUrl}");
        if (message is not null)
            noteLines.Add(message);
        var note = string.Join("\n", noteLines);

        var contact = await dbContext.Set<MarketingContact>()
            .FirstOrDefaultAsync(c => c.Email == normalizedEmail, cancellationToken);

        if (contact is null)
        {
            contact = new MarketingContact
            {
                Id = Guid.NewGuid(),
                Email = normalizedEmail,
                ContactType = MarketingContactType.Broker,
                Status = MarketingContactStatus.Interested,
                Source = "Makler-Anfrage"
            };
            dbContext.Set<MarketingContact>().Add(contact);
        }
        else if (contact.Status is not (MarketingContactStatus.Customer or MarketingContactStatus.DoNotContact))
        {
            // Eine aktive Anfrage ist staerker als jeder Funnel-Zwischenstand -
            // Customer bleibt Customer, DoNotContact wird nicht angetastet
            contact.Status = MarketingContactStatus.Interested;
        }

        // Bereits gepflegte Stammdaten nie mit Formulareingaben ueberschreiben
        contact.Name ??= contactName;
        contact.Company ??= company;
        contact.Phone ??= phone;
        contact.LastReplyAt = now;
        contact.Notes = AppendNote(contact.Notes, note);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            // Unique-Index-Rennen (Doppel-Submit): der Kontakt existiert bereits -
            // fuer den Makler ist die Anfrage damit trotzdem angekommen
            logger.LogWarning(ex, "Makler-Anfrage: Kontakt {Email} parallel angelegt, Notiz verworfen", normalizedEmail);
            return new SubmitBrokerLeadResponse(true, null);
        }

        await NotifyTeamAsync(company, contactName, normalizedEmail, phone, listingsUrl, message, cancellationToken);

        return new SubmitBrokerLeadResponse(true, null);
    }

    private async Task NotifyTeamAsync(
        string company,
        string? contactName,
        string email,
        string? phone,
        string? listingsUrl,
        string? message,
        CancellationToken cancellationToken)
    {
        try
        {
            var lines = new List<(string Label, string Value)> { ("Firma", company) };
            if (contactName is not null)
                lines.Add(("Ansprechpartner", contactName));
            lines.Add(("E-Mail", email));
            if (phone is not null)
                lines.Add(("Telefon", phone));
            if (listingsUrl is not null)
                lines.Add(("Inserate", listingsUrl));
            if (message is not null)
                lines.Add(("Nachricht", message));

            var textBody =
                "Neue Makler-Anfrage über heimatplatz.at/makler/:\n\n"
                + string.Join("\n", lines.Select(l => $"{l.Label}: {l.Value}"))
                + "\n\nDer Kontakt liegt im Intern-Bereich unter Marketing → Kontakte.";

            var htmlRows = string.Join("", lines.Select(l =>
                $"<tr><td style=\"padding:4px 12px 4px 0;color:#666;white-space:nowrap;vertical-align:top;\">{System.Net.WebUtility.HtmlEncode(l.Label)}</td>" +
                $"<td style=\"padding:4px 0;\">{System.Net.WebUtility.HtmlEncode(l.Value).Replace("\n", "<br/>")}</td></tr>"));
            var htmlBody =
                "<div style=\"font-family:Arial,sans-serif;font-size:14px;color:#222;\">" +
                "<p><strong>Neue Makler-Anfrage</strong> über heimatplatz.at/makler/:</p>" +
                $"<table style=\"border-collapse:collapse;\">{htmlRows}</table>" +
                "<p style=\"color:#666;\">Der Kontakt liegt im Intern-Bereich unter Marketing &rarr; Kontakte.</p>" +
                "</div>";

            // Ans eigene Team-Postfach (FromAddress = info@heimatplatz.at)
            await emailSender.SendAsync(new EmailMessage(
                ToAddress: emailOptions.Value.FromAddress,
                Subject: $"Neue Makler-Anfrage: {company}",
                HtmlBody: htmlBody,
                TextBody: textBody
            ), cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Makler-Anfrage von {Email} gespeichert, aber Team-Benachrichtigung fehlgeschlagen", email);
        }
    }

    private static string AppendNote(string? existing, string note)
    {
        var combined = string.IsNullOrWhiteSpace(existing) ? note : $"{existing}\n\n{note}";
        // Aeltestes vorne abschneiden - die neueste Anfrage steht am Ende
        return combined.Length <= MaxNotesLength ? combined : combined[^MaxNotesLength..];
    }

    private static string? NullIfEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string? Truncate(string? value, int maxLength) =>
        value is null || value.Length <= maxLength ? value : value[..maxLength];
}
