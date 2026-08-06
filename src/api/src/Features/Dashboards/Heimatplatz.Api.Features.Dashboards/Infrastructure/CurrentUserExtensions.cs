using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace Heimatplatz.Api.Features.Dashboards.Infrastructure;

/// <summary>
/// User-ID aus dem JWT (sub-Claim) - gleiche Logik wie in den anderen Features,
/// hier einmal zentral statt in jedem Handler dupliziert.
/// </summary>
internal static class CurrentUserExtensions
{
    public static Guid GetRequiredUserId(this IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext ist nicht verfuegbar.");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("Benutzer-ID nicht im Token gefunden.");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
            throw new UnauthorizedAccessException("Ungueltige Benutzer-ID im Token.");

        return userId;
    }
}
