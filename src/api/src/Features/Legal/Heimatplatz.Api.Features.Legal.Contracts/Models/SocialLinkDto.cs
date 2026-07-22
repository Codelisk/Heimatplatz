namespace Heimatplatz.Api.Features.Legal.Contracts.Models;

/// <summary>
/// Ein Social-Media-Profil des Unternehmens. <see cref="Platform"/> dient als Label bzw.
/// Icon-Schluessel im Frontend, <see cref="Url"/> landet zusaetzlich im Organization-JSON-LD
/// (schema.org "sameAs").
/// </summary>
public record SocialLinkDto(
    string Platform,
    string Url
);
