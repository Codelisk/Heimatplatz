using Microsoft.Extensions.Logging;

namespace Heimatplatz.Api.Features.Marketing.Services;

/// <summary>
/// Dev-Provider ohne echte KI: baut einen erkennbaren Platzhalter-Entwurf aus den
/// Stichwoertern, damit der komplette Flow (Generieren -> Nachbearbeiten -> Senden)
/// lokal ohne AiConnector-Zugang testbar ist.
/// </summary>
public class MockMarketingEmailGenerator(ILogger<MockMarketingEmailGenerator> logger) : IMarketingEmailGenerator
{
    /// <summary>
    /// Kennzeichnet Mock-Entwuerfe im Text. SendMarketingEmailHandler verweigert
    /// Bodies mit diesem Marker, damit Platzhaltertext nie an Kunden gehen kann.
    /// </summary>
    public const string PlaceholderMarker = "[PLATZHALTER OHNE KI]";

    public Task<MarketingEmailDraft> GenerateAsync(string keywords, string? recipientName, CancellationToken ct = default)
    {
        var salutation = string.IsNullOrWhiteSpace(recipientName)
            ? "Guten Tag,"
            : $"Guten Tag {recipientName.Trim()},";

        var body = $"""
            {PlaceholderMarker}

            {salutation}

            vielen Dank für Ihr Interesse an Heimatplatz, der Immobilien-Plattform für Oberösterreich.

            Ihre Stichwörter: {keywords.Trim()}

            (Mock-Text ohne KI – am Server erstellt der konfigurierte Provider aus den Stichwörtern den finalen E-Mail-Text. Marketing__Provider=AiConnector plus AiConnector__ApiKey setzen.)

            Mit freundlichen Grüßen
            """;

        logger.LogInformation("[Marketing] Mock-E-Mail-Entwurf erstellt ({Length} Zeichen)", body.Length);
        return Task.FromResult(new MarketingEmailDraft("Heimatplatz: Ihre Anfrage", body));
    }
}
