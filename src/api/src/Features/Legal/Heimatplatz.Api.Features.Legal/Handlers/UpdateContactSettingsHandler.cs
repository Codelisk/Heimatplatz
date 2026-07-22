using System.Net.Mail;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Heimatplatz.Api.Features.Legal.Data.Entities;
using Heimatplatz.Api.Features.Legal.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Handlers;

/// <summary>
/// Speichert die Kontakt-Zusatzfelder (Support-Adresse, Telefon-Override, Erreichbarkeit,
/// Social-Profile). Damit ist die Telefonnummer zur Laufzeit pflegbar - vor diesem Handler
/// brauchte jede Stammdaten-Aenderung eine SQL-REPLACE-Migration.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/legal")]
public class UpdateContactSettingsHandler(
    AppDbContext dbContext,
    IContactInfoProvider contactInfoProvider,
    IAdminAccessGuard accessGuard,
    ILogger<UpdateContactSettingsHandler> logger
) : IRequestHandler<UpdateContactSettingsRequest, UpdateContactSettingsResponse>
{
    [MediatorHttpPost("/contact", OperationId = "UpdateContactSettings")]
    public async Task<UpdateContactSettingsResponse> Handle(UpdateContactSettingsRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        // Fachliche Fehler als Success=false statt Exception - die Intern-Seite zeigt sie
        // dem Bearbeiter an (gleiches Muster wie SendMarketingEmailHandler)
        if (InvalidEmail(request.Email, out var emailError))
            return Failed($"Die allgemeine E-Mail-Adresse ist ungültig: {emailError}");

        if (InvalidEmail(request.SupportEmail, out var supportError))
            return Failed($"Die Support-E-Mail-Adresse ist ungültig: {supportError}");

        if (InvalidUrl(request.Website))
            return Failed("Die Website muss mit http:// oder https:// beginnen.");

        foreach (var link in request.SocialLinks ?? [])
        {
            if (string.IsNullOrWhiteSpace(link.Url))
                continue;

            if (InvalidUrl(link.Url))
                return Failed($"Der Link für \"{link.Platform}\" muss mit http:// oder https:// beginnen.");
        }

        var settings = new ContactSettingsDto(
            Email: Normalize(request.Email),
            SupportEmail: Normalize(request.SupportEmail),
            Phone: Normalize(request.Phone),
            Website: Normalize(request.Website),
            OfficeHours: Normalize(request.OfficeHours),
            SocialLinks: NormalizeLinks(request.SocialLinks)
        );

        var entity = await dbContext.Set<LegalSettings>()
            .FirstOrDefaultAsync(x => x.SettingType == LegalSettingTypes.Contact && x.IsActive, cancellationToken);

        if (entity == null)
        {
            // Datenbanken von vor dem Contact-Seeder haben den Datensatz noch nicht
            entity = new LegalSettings
            {
                SettingType = LegalSettingTypes.Contact,
                Version = "1.0",
                EffectiveDate = DateTimeOffset.UtcNow,
                IsActive = true
            };
            dbContext.Set<LegalSettings>().Add(entity);
        }

        entity.ResponsiblePartyJson = LegalJson.Serialize(settings);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Admin] Kontaktdaten aktualisiert (Telefon {PhoneState}, Support-Mail {SupportState})",
            settings.Phone is null ? "leer" : "gesetzt",
            settings.SupportEmail is null ? "leer" : "gesetzt");

        var contact = await contactInfoProvider.GetAsync(cancellationToken);
        return new UpdateContactSettingsResponse(true, null, contact);
    }

    private static UpdateContactSettingsResponse Failed(string error)
        => new(false, error, null);

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static List<SocialLinkDto>? NormalizeLinks(List<SocialLinkDto>? links)
    {
        var cleaned = (links ?? [])
            .Where(link => !string.IsNullOrWhiteSpace(link.Platform) && !string.IsNullOrWhiteSpace(link.Url))
            .Select(link => new SocialLinkDto(link.Platform.Trim(), link.Url.Trim()))
            .ToList();

        return cleaned.Count == 0 ? null : cleaned;
    }

    private static bool InvalidEmail(string? value, out string reason)
    {
        reason = string.Empty;
        if (string.IsNullOrWhiteSpace(value))
            return false;

        if (MailAddress.TryCreate(value.Trim(), out _))
            return false;

        reason = value.Trim();
        return true;
    }

    private static bool InvalidUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return !Uri.TryCreate(value.Trim(), UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps);
    }
}
