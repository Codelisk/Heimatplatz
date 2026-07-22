using Heimatplatz.Maui.Features.Feedback.Presentation;
using Heimatplatz.Maui.Features.Properties.Presentation;
using Microsoft.Extensions.Logging;
using Shiny;

namespace Heimatplatz.Maui.Core.DeepLink;

/// <summary>
/// Service fuer das Handling von Deep Links.
/// Unterstuetzte Schemas:
/// - heimatplatz://property/{guid} -> Route "PropertyDetail"
/// - heimatplatz://foreclosure/{guid} -> Route "ForeclosureDetail"
/// - heimatplatz://feedback/{guid} -> Route "FeedbackThread" (Push bei Team-Antwort)
/// Navigiert via Shiny INavigator und setzt die ShellProperty stark typisiert.
/// </summary>
[Singleton]
public class DeepLinkService(
    INavigator navigator,
    ILogger<DeepLinkService> logger) : IDeepLinkService
{
    private const string Scheme = "heimatplatz";
    private const string PropertyHost = "property";
    private const string ForeclosureHost = "foreclosure";
    private const string FeedbackHost = "feedback";

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
                PropertyHost => await NavigateToPropertyAsync(propertyId),
                ForeclosureHost => await NavigateToForeclosureAsync(propertyId),
                FeedbackHost => await NavigateToFeedbackAsync(propertyId),
                _ => HandleUnknownHost(host)
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[DeepLink] Error handling deep link: {Uri}", uri);
            return false;
        }
    }

    private async Task<bool> NavigateToPropertyAsync(Guid propertyId)
    {
        logger.LogInformation("[DeepLink] Navigating to PropertyDetail: {PropertyId}", propertyId);
        await navigator.NavigateTo<PropertyDetailViewModel>(
            viewModel => viewModel.PropertyId = propertyId.ToString("D"));
        return true;
    }

    private async Task<bool> NavigateToForeclosureAsync(Guid propertyId)
    {
        logger.LogInformation("[DeepLink] Navigating to ForeclosureDetail: {PropertyId}", propertyId);
        await navigator.NavigateTo<ForeclosureDetailViewModel>(
            viewModel => viewModel.PropertyId = propertyId.ToString("D"));
        return true;
    }

    private async Task<bool> NavigateToFeedbackAsync(Guid ticketId)
    {
        logger.LogInformation("[DeepLink] Navigating to FeedbackThread: {TicketId}", ticketId);
        await navigator.NavigateTo<FeedbackThreadViewModel>(
            viewModel => viewModel.TicketId = ticketId.ToString("D"));
        return true;
    }

    private bool HandleUnknownHost(string host)
    {
        logger.LogWarning("[DeepLink] Unknown host: {Host}", host);
        return false;
    }
}
