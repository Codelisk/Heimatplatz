using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Partners.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Partners.Data.Entities;
using Heimatplatz.Api.Features.Partners.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Partners.Handlers;

/// <summary>
/// Legt einen Partner an oder ersetzt einen bestehenden komplett (das Intern-Formular
/// schickt alle Felder vorbefuellt zurueck). Fachliche Fehler als Success=false statt
/// Exception - gleiches Muster wie UpdateContactSettingsHandler.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/partners")]
public class SavePartnerHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    ILogger<SavePartnerHandler> logger
) : IRequestHandler<SavePartnerRequest, SavePartnerResponse>
{
    [MediatorHttpPost("/save", OperationId = "SavePartner")]
    public async Task<SavePartnerResponse> Handle(SavePartnerRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        var error = PartnerValidation.Validate(request);
        if (error != null)
            return new SavePartnerResponse(false, error, null);

        Partner? partner = null;
        if (request.Id is { } id)
        {
            partner = await dbContext.Set<Partner>()
                .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

            if (partner == null)
                return new SavePartnerResponse(false, "Der Partner wurde nicht gefunden (eventuell inzwischen gelöscht).", null);
        }

        if (partner == null)
        {
            partner = new Partner { Name = request.Name.Trim(), Category = request.Category };
            dbContext.Set<Partner>().Add(partner);
        }

        partner.Name = request.Name.Trim();
        partner.Category = request.Category;
        partner.Description = Normalize(request.Description);
        partner.WebsiteUrl = Normalize(request.WebsiteUrl);
        partner.LogoUrl = Normalize(request.LogoUrl);
        partner.Region = Normalize(request.Region);
        partner.PartnerSinceYear = request.PartnerSinceYear;
        partner.SourceName = Normalize(request.SourceName);
        partner.SellerName = Normalize(request.SellerName);
        partner.DisplayOrder = request.DisplayOrder;
        partner.IsVisible = request.IsVisible;

        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogInformation("[Admin] Partner {Action}: {Name} (sichtbar: {Visible})",
            request.Id == null ? "angelegt" : "aktualisiert", partner.Name, partner.IsVisible);

        return new SavePartnerResponse(true, null, partner.Id);
    }

    private static string? Normalize(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
