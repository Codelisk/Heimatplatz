using Heimatplatz.Api.Cleanup;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Dashboards.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Heimatplatz.Api.Features.Dashboards.Services;

/// <summary>
/// Loescht im Rahmen der Konto-Loeschung (DSGVO Art. 17) alle Uebersichten des
/// Benutzers inklusive Revisionen. Registrierung erfolgt in <c>AddDashboardsFeature</c>.
/// </summary>
public class DashboardsUserDataEraser(
    AppDbContext dbContext
) : IUserDataEraser
{
    /// <summary>Keine Abhaengigkeiten zu anderen Features - hinter den bestehenden Erasern (40).</summary>
    public int Order => 50;

    public async Task EraseUserDataAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Revisionen zuerst (FK auf UserDashboards) - ExecuteDelete umgeht die Kaskade
        var dashboardIds = dbContext.Set<UserDashboard>()
            .Where(d => d.UserId == userId)
            .Select(d => d.Id);

        await dbContext.Set<UserDashboardRevision>()
            .Where(r => dashboardIds.Contains(r.DashboardId))
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.Set<UserDashboard>()
            .Where(d => d.UserId == userId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}
