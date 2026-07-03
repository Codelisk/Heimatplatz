using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Core.DeepLink;

/// <summary>
/// Service fuer das Handling von Deep Links.
/// Unterstuetzte Schemas:
/// - heimatplatz://property/{guid} -> Route "PropertyDetail"
/// - heimatplatz://foreclosure/{guid} -> Route "ForeclosureDetail"
/// Navigiert via Shiny INavigator (ShellProperty-Konvention: Parameter "PropertyId").
/// </summary>
[Singleton]
public class DeepLinkService(
    INavigator navigator,
    ILogger<DeepLinkService> logger) : IDeepLinkService
{
    private const string Scheme = "heimatplatz";
    private const string PropertyHost = "property";
    private const string ForeclosureHost = "foreclosure";

    private const string PropertyDetailRoute = "PropertyDetail";
    private const string ForeclosureDetailRoute = "ForeclosureDetail";

    /// <inheritdoc />
    public bool CanHandleUri(Uri uri)
        => uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public async Task<bool> HandleDeepLinkAsync(Uri uri)
    {
        if (!CanHandleUri(uri))
        {
            logger.LogWarning("[DeepLink] Cannot handle URI: {Uri}", uri);
            return false;
        }

        logger.LogInformation("[DeepLink] Handling deep link: {Uri}", uri);

        try
        {
            var host = uri.Host.ToLowerInvariant();
            var pathSegments = uri.AbsolutePath.Trim('/').Split('/');

            // Parse GUID from path
            if (pathSegments.Length == 0 || !Guid.TryParse(pathSegments[0], out var propertyId))
            {
                logger.LogWarning("[DeepLink] Invalid path, expected GUID: {Path}", uri.AbsolutePath);
                return false;
            }

            return host switch
            {
                PropertyHost => await NavigateAsync(PropertyDetailRoute, propertyId),
                ForeclosureHost => await NavigateAsync(ForeclosureDetailRoute, propertyId),
                _ => HandleUnknownHost(host)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DeepLink] Error handling deep link: {Uri}", uri);
            return false;
        }
    }

    private async Task<bool> NavigateAsync(string route, Guid propertyId)
    {
        logger.LogInformation("[DeepLink] Navigating to {Route}: {PropertyId}", route, propertyId);
        await navigator.NavigateTo(route, args: [("PropertyId", propertyId.ToString())]);
        return true;
    }

    private bool HandleUnknownHost(string host)
    {
        logger.LogWarning("[DeepLink] Unknown host: {Host}", host);
        return false;
    }
}
