using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Exceptions;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Properties.Contracts.Enums;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Shiny;

namespace Heimatplatz.Api.Features.Properties.Services;

/// <inheritdoc />
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class SellerInfoResolver(AppDbContext dbContext) : ISellerInfoResolver
{
    public async Task<SellerInfo> ResolveForUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Set<User>()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new UnauthorizedAccessException("Benutzer nicht gefunden.");

        // Die RequireSeller-Policy garantiert das normalerweise schon - defensiv pruefen,
        // falls das Profil seit Token-Ausstellung geaendert wurde.
        if (user.SellerType == null)
        {
            throw new ValidationException("Nur Verkaeufer koennen Inserate anlegen. Bitte hinterlegen Sie in Ihrem Profil einen Anbietertyp.");
        }

        var sellerType = user.SellerType.Value;

        var sellerName = sellerType is SellerType.Broker or SellerType.PropertyManager
            ? user.CompanyName ?? user.FullName
            : user.FullName;

        var sellerSourceId = await ResolveSellerSourceAsync(sellerType, sellerName, cancellationToken);

        var contactType = sellerType switch
        {
            SellerType.Broker => ContactType.Agent,
            SellerType.PropertyManager => ContactType.PropertyManager,
            _ => ContactType.Seller
        };

        return new SellerInfo(sellerType, sellerName, sellerSourceId, contactType);
    }

    public async Task<Guid?> ResolveSellerSourceAsync(SellerType sellerType, string sellerName, CancellationToken cancellationToken = default)
    {
        // Nur gewerbliche Anbieter (Makler/Verwaltung) landen im Anbieter-Ausschlussfilter
        if (sellerType is not (SellerType.Broker or SellerType.PropertyManager))
            return null;

        var trimmed = sellerName.Trim();
        if (trimmed.Length == 0)
            return null;

        // Erst lokal (noch nicht gespeicherte Entities aus demselben Batch), dann in der DB suchen
        var local = dbContext.Set<SellerSource>().Local
            .FirstOrDefault(ss => string.Equals(ss.Name, trimmed, StringComparison.OrdinalIgnoreCase));

        if (local != null)
            return local.Id;

        var existing = await dbContext.Set<SellerSource>()
            .FirstOrDefaultAsync(ss => ss.Name.ToLower() == trimmed.ToLower(), cancellationToken);

        if (existing != null)
            return existing.Id;

        var source = new SellerSource
        {
            Id = Guid.NewGuid(),
            Name = trimmed,
            IsDefault = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<SellerSource>().Add(source);
        // Kein SaveChanges hier: der aufrufende Handler speichert atomar mit dem Inserat
        return source.Id;
    }
}
