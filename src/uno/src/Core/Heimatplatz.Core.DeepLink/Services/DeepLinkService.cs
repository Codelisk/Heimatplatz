using Heimatplatz.Features.Properties.Contracts.Models;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.DependencyInjection;
using Uno.Extensions.Navigation;

namespace Heimatplatz.Core.DeepLink.Services;

/// <summary>
/// Service fuer das Handling von Deep Links.
/// Unterstuetzte Schemas:
/// - heimatplatz://property/{guid} -> PropertyDetailPage
/// - heimatplatz://foreclosure/{guid} -> ForeclosureDetailPage
/// </summary>
[Service(UnoService.Lifetime, TryAdd = UnoService.TryAdd)]
public class DeepLinkService : IDeepLinkService
{
    private const string Scheme = "heimatplatz";
    private const string PropertyHost = "property";
    private const string ForeclosureHost = "foreclosure";

    private readonly ILogger<DeepLinkService> _logger;
    private readonly IServiceProvider _serviceProvider;

    public DeepLinkService(
        ILogger<DeepLinkService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <inheritdoc />
    public bool CanHandleUri(Uri uri)
    {
        if (uri == null)
            return false;

        return uri.Scheme.Equals(Scheme, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task<bool> HandleDeepLinkAsync(Uri uri)
    {
        if (!CanHandleUri(uri))
        {
            _logger.LogWarning("[DeepLink] Cannot handle URI: {Uri}", uri);
            return false;
        }

        _logger.LogInformation("[DeepLink] Handling deep link: {Uri}", uri);

        try
        {
            var host = uri.Host.ToLowerInvariant();
            var pathSegments = uri.AbsolutePath.Trim('/').Split('/');

            // Parse GUID from path
            if (pathSegments.Length == 0 || !Guid.TryParse(pathSegments[0], out var propertyId))
            {
                _logger.LogWarning("[DeepLink] Invalid path, expected GUID: {Path}", uri.AbsolutePath);
                return false;
            }

            return host switch
            {
                PropertyHost => await NavigateToPropertyAsync(propertyId),
                ForeclosureHost => await NavigateToForeclosureAsync(propertyId),
                _ => HandleUnknownHost(host)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[DeepLink] Error handling deep link: {Uri}", uri);
            return false;
        }
    }

    private async Task<bool> NavigateToPropertyAsync(Guid propertyId)
    {
        _logger.LogInformation("[DeepLink] Navigating to PropertyDetail: {PropertyId}", propertyId);

        var navigator = _serviceProvider.GetService<INavigator>();
        if (navigator == null)
        {
            _logger.LogError("[DeepLink] INavigator service not available");
            return false;
        }

        var data = new PropertyDetailData(propertyId);
        await navigator.NavigateRouteAsync(this, "PropertyDetail", data: data);
        return true;
    }

    private async Task<bool> NavigateToForeclosureAsync(Guid propertyId)
    {
        _logger.LogInformation("[DeepLink] Navigating to ForeclosureDetail: {PropertyId}", propertyId);

        var navigator = _serviceProvider.GetService<INavigator>();
        if (navigator == null)
        {
            _logger.LogError("[DeepLink] INavigator service not available");
            return false;
        }

        var data = new ForeclosureDetailData(propertyId);
        await navigator.NavigateRouteAsync(this, "ForeclosureDetail", data: data);
        return true;
    }

    private bool HandleUnknownHost(string host)
    {
        _logger.LogWarning("[DeepLink] Unknown host: {Host}", host);
        return false;
    }
}
