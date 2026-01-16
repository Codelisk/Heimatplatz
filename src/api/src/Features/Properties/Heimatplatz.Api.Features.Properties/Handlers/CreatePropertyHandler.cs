using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Properties.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Properties.Data.Entities;
using Microsoft.AspNetCore.Http;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Properties.Handlers;

/// <summary>
/// Handler zum Erstellen einer neuen Immobilie
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class CreatePropertyHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<CreatePropertyRequest, CreatePropertyResponse>
{
    [MediatorHttpPost("/api/properties", OperationId = "CreateProperty")]
    public async Task<CreatePropertyResponse> Handle(CreatePropertyRequest request, IMediatorContext context, CancellationToken cancellationToken)
    {
        // UserId aus JWT Token extrahieren
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token");
        }

        // Validierung
        if (string.IsNullOrWhiteSpace(request.Titel) || request.Titel.Length < 10 || request.Titel.Length > 200)
        {
            throw new ArgumentException("Titel muss zwischen 10 und 200 Zeichen lang sein", nameof(request.Titel));
        }

        if (request.Preis <= 0 || request.Preis > 100_000_000)
        {
            throw new ArgumentException("Preis muss groesser als 0 und kleiner als 100'000'000 sein", nameof(request.Preis));
        }

        if (string.IsNullOrWhiteSpace(request.Beschreibung) || request.Beschreibung.Length < 50 || request.Beschreibung.Length > 2000)
        {
            throw new ArgumentException("Beschreibung muss zwischen 50 und 2000 Zeichen lang sein", nameof(request.Beschreibung));
        }

        // Property erstellen
        var property = new Property
        {
            Id = Guid.NewGuid(),
            Titel = request.Titel.Trim(),
            Adresse = request.Adresse.Trim(),
            Ort = request.Ort.Trim(),
            Plz = request.Plz.Trim(),
            Preis = request.Preis,
            Typ = request.Typ,
            AnbieterTyp = request.AnbieterTyp,
            AnbieterName = request.AnbieterName.Trim(),
            Beschreibung = request.Beschreibung?.Trim(),
            WohnflaecheM2 = request.WohnflaecheM2,
            GrundstuecksflaecheM2 = request.GrundstuecksflaecheM2,
            Zimmer = request.Zimmer,
            Baujahr = request.Baujahr,
            Ausstattung = request.Ausstattung ?? new List<string>(),
            BildUrls = request.BildUrls ?? new List<string>(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<Property>().Add(property);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreatePropertyResponse(
            property.Id,
            property.Titel,
            property.CreatedAt
        );
    }
}
