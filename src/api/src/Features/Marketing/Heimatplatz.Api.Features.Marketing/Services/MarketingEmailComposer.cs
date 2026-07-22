using System.Net;
using System.Text;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Signatur-Quelle sind die gepflegten Kontaktdaten (GetContactInfoRequest -> Impressum
/// plus Contact-Overrides), also exakt das, was auch im Web-Footer steht - kein zweiter
/// Pflegeort. Bewusst NICHT das rohe Impressum: sonst wuerde eine unter /intern/kontakt
/// hinterlegte abweichende Kontaktadresse in der Signatur ignoriert.
///
/// Nicht gepflegte Angaben werden weggelassen statt durch einen Platzhalter ersetzt - eine
/// hartcodierte Adresse in der Signatur waere genau die Drift, die das Feature abschafft.
/// Layout-Konventionen wie die Auth-Mails (Arial, Markenrot #b3261e, 560px).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class MarketingEmailComposer(IMediator mediator) : IMarketingEmailComposer
{
    private const string BrandName = "Heimatplatz";
    private const string BrandTagline = "Immobilien in Oberösterreich";
    private const string BrandRed = "#b3261e";

    public async Task<string> GetSignatureTextAsync(CancellationToken ct = default)
    {
        var contact = await LoadContactAsync(ct);

        // Bewusst '\n' statt AppendLine: deterministische Zeilenenden unabhaengig
        // vom Server-OS (Windows-Dev vs. Linux-Prod).
        var lines = new List<string>
        {
            "-- ",
            $"{BrandName} – {BrandTagline}"
        };
        if (!string.IsNullOrWhiteSpace(contact?.CompanyName))
            lines.Add(contact.CompanyName);
        if (BuildAddressLine(contact) is { } address)
            lines.Add(address);
        if (!string.IsNullOrWhiteSpace(contact?.Phone))
            lines.Add($"Telefon: {contact.Phone}");
        if (!string.IsNullOrWhiteSpace(contact?.SupportEmail))
            lines.Add($"E-Mail: {contact.SupportEmail}");
        if (WebsiteDisplay(contact) is { } website)
            lines.Add($"Web: {website}");
        return string.Join('\n', lines);
    }

    public async Task<EmailMessage> ComposeAsync(string toAddress, string subject, string body, string? ccAddress = null, string? bccAddress = null, CancellationToken ct = default)
    {
        var contact = await LoadContactAsync(ct);
        var signatureText = await GetSignatureTextAsync(ct);

        var text = $"{body.Trim()}\n\n{signatureText}";
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#1f1f1f">
            {BodyAsHtml(body)}
            {SignatureHtml(contact)}
            </div>
            """;

        // Marketing-Mails sollen wie von Hand verschickt im Webmail (Gesendet-Ordner)
        // auftauchen - Auth-Mails lassen das bewusst aus.
        return new EmailMessage(toAddress, subject.Trim(), html, text, ArchiveToSentFolder: true, CcAddress: ccAddress, BccAddress: bccAddress);
    }

    private async Task<LegalContactInfoDto?> LoadContactAsync(CancellationToken ct)
    {
        var response = await mediator.Request(new GetContactInfoRequest(), ct);
        return response.Result.Contact;
    }

    private static string BodyAsHtml(string body)
    {
        var paragraphs = body.Replace("\r\n", "\n").Trim()
            .Split("\n\n", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var sb = new StringBuilder();
        foreach (var paragraph in paragraphs)
        {
            var encoded = WebUtility.HtmlEncode(paragraph).Replace("\n", "<br>");
            sb.AppendLine($"""  <p style="margin:0 0 14px;line-height:1.55">{encoded}</p>""");
        }
        return sb.ToString().TrimEnd();
    }

    private static string SignatureHtml(LegalContactInfoDto? info)
    {
        var lines = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(info?.CompanyName))
            lines.AppendLine($"""    <p style="margin:0">{WebUtility.HtmlEncode(info.CompanyName)}</p>""");
        if (BuildAddressLine(info) is { } address)
            lines.AppendLine($"""    <p style="margin:0">{WebUtility.HtmlEncode(address)}</p>""");

        // Jede Zeile nur bei gepflegtem Wert - lieber eine kuerzere Signatur als eine
        // erfundene Adresse
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(info?.Phone))
        {
            var phoneLink = string.IsNullOrWhiteSpace(info.PhoneLink) ? null : info.PhoneLink;
            var phoneText = WebUtility.HtmlEncode(info.Phone);
            parts.Add(phoneLink is null
                ? $"Telefon: {phoneText}"
                : $"""Telefon: <a href="tel:{phoneLink}" style="color:{BrandRed};text-decoration:none">{phoneText}</a>""");
        }
        if (!string.IsNullOrWhiteSpace(info?.SupportEmail))
        {
            var email = WebUtility.HtmlEncode(info.SupportEmail);
            parts.Add($"""E-Mail: <a href="mailto:{email}" style="color:{BrandRed};text-decoration:none">{email}</a>""");
        }
        if (WebsiteDisplay(info) is { } website && !string.IsNullOrWhiteSpace(info?.Website))
            parts.Add($"""Web: <a href="{WebUtility.HtmlEncode(info.Website)}" style="color:{BrandRed};text-decoration:none">{WebUtility.HtmlEncode(website)}</a>""");

        var contact = string.Join("<br>", parts);

        return $"""
              <div style="margin-top:32px;padding-top:14px;border-top:2px solid {BrandRed};font-size:13px;line-height:1.5;color:#1f1f1f">
                <p style="margin:0;font-size:15px;font-weight:bold;color:{BrandRed}">{BrandName}</p>
                <p style="margin:2px 0 10px;color:#6b6b6b">{BrandTagline}</p>
            {lines.ToString().TrimEnd()}
                <p style="margin:10px 0 0">{contact}</p>
              </div>
            """;
    }

    private static string? BuildAddressLine(LegalContactInfoDto? info)
    {
        if (info is null || string.IsNullOrWhiteSpace(info.Street))
            return null;

        var cityPart = string.Join(" ", new[] { info.PostalCode, info.City }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        var parts = new[] { info.Street, cityPart, info.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(", ", parts);
    }

    private static string? WebsiteDisplay(LegalContactInfoDto? info)
    {
        if (string.IsNullOrWhiteSpace(info?.Website))
            return null;

        return info.Website.Replace("https://", "").Replace("http://", "").TrimEnd('/');
    }
}
