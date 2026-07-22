using Heimatplatz.Api.Features.Legal.Contracts.Models;

namespace Heimatplatz.Api.Features.Legal.Services;

/// <summary>
/// Fuehrt Impressum und Contact-Zusatzfelder zu den Kontaktdaten zusammen, die die
/// Frontends bekommen.
///
/// Regel: Firma und Adresse kommen IMMER aus dem Impressum (eine Quelle fuer die
/// Pflichtangaben), E-Mail/Telefon/Website duerfen im Contact-Datensatz ueberschrieben
/// werden, SupportEmail/OfficeHours/SocialLinks gibt es nur dort. Leere Strings gelten
/// ueberall als "nicht gepflegt".
/// </summary>
public static class ContactInfoFactory
{
    public static ContactInfoDto Create(ImprintPartyDto imprint, ContactSettingsDto? contact)
    {
        var email = Coalesce(contact?.Email, imprint.Email);
        var phone = Coalesce(contact?.Phone, imprint.Phone);

        return new ContactInfoDto(
            CompanyName: imprint.CompanyName,
            Street: imprint.Street,
            PostalCode: imprint.PostalCode,
            City: imprint.City,
            Country: imprint.Country,
            Email: email ?? string.Empty,
            // Nie leer: ohne eigene Support-Adresse gilt die allgemeine
            SupportEmail: Coalesce(contact?.SupportEmail, email) ?? string.Empty,
            Phone: phone,
            PhoneLink: PhoneNumberFormatter.ToTelLink(phone),
            Website: Coalesce(contact?.Website, imprint.Website),
            OfficeHours: Clean(contact?.OfficeHours),
            SocialLinks: CleanLinks(contact?.SocialLinks)
        );
    }

    private static string? Coalesce(params string?[] candidates)
        => candidates.Select(Clean).FirstOrDefault(value => value is not null);

    private static string? Clean(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<SocialLinkDto> CleanLinks(List<SocialLinkDto>? links)
        => links is null
            ? []
            : [.. links
                .Where(link => !string.IsNullOrWhiteSpace(link.Platform) && !string.IsNullOrWhiteSpace(link.Url))
                .Select(link => new SocialLinkDto(link.Platform.Trim(), link.Url.Trim()))];
}
