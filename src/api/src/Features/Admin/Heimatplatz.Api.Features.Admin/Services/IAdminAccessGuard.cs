namespace Heimatplatz.Api.Features.Admin.Services;

/// <summary>
/// Prueft den Shared-Key-Zugang zu den /api/admin-Endpoints.
/// </summary>
public interface IAdminAccessGuard
{
    /// <summary>
    /// Wirft <see cref="UnauthorizedAccessException"/> (=&gt; 401), wenn der
    /// X-Admin-Key-Header fehlt oder nicht zum konfigurierten Admin:ApiKey passt.
    /// </summary>
    void EnsureAuthorized();
}
