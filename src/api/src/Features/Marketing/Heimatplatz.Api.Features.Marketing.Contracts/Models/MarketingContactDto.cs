namespace Heimatplatz.Api.Features.Marketing.Contracts.Models;

/// <summary>
/// Kontakt der Marketing-Kontaktdatenbank fuer Listen und Detail-Ansicht.
/// <see cref="Email"/> ist optional: Kontakte aus dem Firmenpool entstehen ohne Adresse
/// (Firmenbuch fuehrt keine Kontaktdaten) und werden erst beim Telefonat ergaenzt.
/// <see cref="Name"/> ist der Anzeigename (aus Titel/Vorname/Nachname zusammengesetzt bzw.
/// Alt-Bestand); <see cref="Salutation"/>/<see cref="Title"/>/<see cref="FirstName"/>/
/// <see cref="LastName"/> sind die strukturierten Felder, aus denen der
/// MarketingTemplateRenderer die Anrede deterministisch baut.
/// </summary>
public record MarketingContactDto(
    Guid Id,
    string? Email,
    string? Name,
    MarketingSalutation Salutation,
    string? Title,
    string? FirstName,
    string? LastName,
    string? Company,
    string? Phone,
    string? City,
    MarketingContactType ContactType,
    MarketingContactStatus Status,
    string? Notes,
    string? Source,
    string? FirmenbuchFnr,
    DateTimeOffset? NextFollowUpAt,
    DateTimeOffset? LastContactedAt,
    DateTimeOffset? LastReplyAt,
    DateTimeOffset CreatedAt,
    int EmailCount,
    int ReplyCount
);
