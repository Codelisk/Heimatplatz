using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Notifications.Data.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny.Extensions.DependencyInjection;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Handlers;

/// <summary>
/// Handler to register a device for push notifications
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/notifications")]
public class RegisterDeviceHandler(
    AppDbContext dbContext,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<RegisterDeviceRequest, RegisterDeviceResponse>
{
    [MediatorHttpPost("/register-device", OperationId = "RegisterDevice")]
    public async Task<RegisterDeviceResponse> Handle(
        RegisterDeviceRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        // Get user ID from JWT token
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new InvalidOperationException("HttpContext is not available");

        var userIdClaim = httpContext.User.FindFirst(JwtRegisteredClaimNames.Sub)
            ?? throw new UnauthorizedAccessException("User ID not found in token");

        if (!Guid.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid User ID in token");
        }

        // Check if subscription already exists
        var existingSubscription = await dbContext.Set<PushSubscription>()
            .FirstOrDefaultAsync(ps => ps.DeviceToken == request.DeviceToken, cancellationToken);

        if (existingSubscription != null)
        {
            // Update existing subscription
            existingSubscription.UserId = userId;
            existingSubscription.Platform = request.Platform;
            existingSubscription.SubscribedAt = DateTimeOffset.UtcNow;
        }
        else
        {
            // Create new subscription
            var subscription = new PushSubscription
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DeviceToken = request.DeviceToken,
                Platform = request.Platform,
                SubscribedAt = DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow
            };

            dbContext.Set<PushSubscription>().Add(subscription);
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return new RegisterDeviceResponse(true);
    }
}
