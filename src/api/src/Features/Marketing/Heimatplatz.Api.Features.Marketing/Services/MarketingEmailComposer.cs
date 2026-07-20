using System.Net;
using System.Text;
using Heimatplatz.Api.Core.Email;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Signatur-Quelle ist das aktive Impressum (GetImprintRequest -> LegalSettings) -
/// dieselben Kontaktdaten wie auf /impressum, kein zweiter Pflegeort. Faellt bei
/// fehlendem Impressum auf die Basis-Kontaktdaten zurueck.
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
        var imprint = await LoadImprintAsync(ct);

        // Bewusst '\n' statt AppendLine: deterministische Zeilenenden unabhaengig
        // vom Server-OS (Windows-Dev vs. Linux-Prod).
        var lines = new List<string>
        {
            "-- ",
            $"{BrandName} – {BrandTagline}"
        };
        if (!string.IsNullOrWhiteSpace(imprint?.CompanyName))
            lines.Add(imprint.CompanyName);
        if (BuildAddressLine(imprint) is { } address)
            lines.Add(address);
        if (!string.IsNullOrWhiteSpace(imprint?.Phone))
            lines.Add($"Telefon: {imprint.Phone}");
        lines.Add($"E-Mail: {imprint?.Email ?? "info@heimatplatz.at"}");
        lines.Add($"Web: {WebsiteDisplay(imprint)}");
        return string.Join('\n', lines);
    }

    public async Task<EmailMessage> ComposeAsync(string toAddress, string subject, string body, CancellationToken ct = default)
    {
        var imprint = await LoadImprintAsync(ct);
        var signatureText = await GetSignatureTextAsync(ct);

        var text = $"{body.Trim()}\n\n{signatureText}";
        var html = $"""
            <div style="font-family:Arial,Helvetica,sans-serif;max-width:560px;margin:0 auto;color:#1f1f1f">
            {BodyAsHtml(body)}
            {SignatureHtml(imprint)}
            </div>
            """;

        return new EmailMessage(toAddress, subject.Trim(), html, text);
    }

    private async Task<ImprintDto?> LoadImprintAsync(CancellationToken ct)
    {
        var response = await mediator.Request(new GetImprintRequest(), ct);
        return response.Result.Imprint;
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

    private static string SignatureHtml(ImprintDto? imprint)
    {
        var email = imprint?.Email ?? "info@heimatplatz.at";
        var websiteUrl = string.IsNullOrWhiteSpace(imprint?.Website) ? "https://www.heimatplatz.at" : imprint.Website;

        var lines = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(imprint?.CompanyName))
            lines.AppendLine($"""    <p style="margin:0">{WebUtility.HtmlEncode(imprint.CompanyName)}</p>""");
        if (BuildAddressLine(imprint) is { } address)
            lines.AppendLine($"""    <p style="margin:0">{WebUtility.HtmlEncode(address)}</p>""");

        var contact = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(imprint?.Phone))
            contact.Append($"""Telefon: {WebUtility.HtmlEncode(imprint.Phone)}<br>""");
        contact.Append($"""E-Mail: <a href="mailto:{email}" style="color:{BrandRed};text-decoration:none">{email}</a><br>""");
        contact.Append($"""Web: <a href="{websiteUrl}" style="color:{BrandRed};text-decoration:none">{WebsiteDisplay(imprint)}</a>""");

        return $"""
              <div style="margin-top:32px;padding-top:14px;border-top:2px solid {BrandRed};font-size:13px;line-height:1.5;color:#1f1f1f">
                <p style="margin:0;font-size:15px;font-weight:bold;color:{BrandRed}">{BrandName}</p>
                <p style="margin:2px 0 10px;color:#6b6b6b">{BrandTagline}</p>
            {lines.ToString().TrimEnd()}
                <p style="margin:10px 0 0">{contact}</p>
              </div>
            """;
    }

    private static string? BuildAddressLine(ImprintDto? imprint)
    {
        if (imprint is null || string.IsNullOrWhiteSpace(imprint.Street))
            return null;

        var cityPart = string.Join(" ", new[] { imprint.PostalCode, imprint.City }
            .Where(part => !string.IsNullOrWhiteSpace(part)));
        var parts = new[] { imprint.Street, cityPart, imprint.Country }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return string.Join(", ", parts);
    }

    private static string WebsiteDisplay(ImprintDto? imprint)
    {
        var url = string.IsNullOrWhiteSpace(imprint?.Website) ? "https://www.heimatplatz.at" : imprint.Website;
        return url.Replace("https://", "").Replace("http://", "").TrimEnd('/');
    }
}
