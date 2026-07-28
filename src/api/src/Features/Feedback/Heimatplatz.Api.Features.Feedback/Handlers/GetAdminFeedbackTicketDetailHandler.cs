using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Admin.Services;
using Heimatplatz.Api.Features.Auth.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Intern: kompletter Verlauf einer Anfrage inkl. Nutzer-Infos. Das Abrufen markiert
/// neue Nutzer-Nachrichten als vom Team gelesen (Badge in der Liste erlischt).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/admin/feedback")]
public class GetAdminFeedbackTicketDetailHandler(
    AppDbContext dbContext,
    IAdminAccessGuard accessGuard,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<GetAdminFeedbackTicketDetailRequest, GetAdminFeedbackTicketDetailResponse>
{
    [MediatorHttpGet("/{Id}", OperationId = "GetAdminFeedbackTicketDetail")]
    public async Task<GetAdminFeedbackTicketDetailResponse> Handle(
        GetAdminFeedbackTicketDetailRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        accessGuard.EnsureAuthorized();

        // Tracked, weil das Abrufen das Team-Unread-Flag zuruecksetzt
        var ticket = await dbContext.Set<FeedbackTicket>()
            .Include(t => t.Messages)
            .ThenInclude(m => m.Attachments)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket == null)
            return new GetAdminFeedbackTicketDetailResponse(null);

        var user = await dbContext.Set<User>()
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == ticket.UserId, cancellationToken);

        var baseUrl = FeedbackMapping.GetBaseUrl(httpContextAccessor, configuration);
        var dto = new AdminFeedbackTicketDetailDto(
            ticket.Id,
            ticket.Category,
            ticket.Subject,
            ticket.Status,
            ticket.CreatedAt,
            ticket.UserId,
            user?.FullName,
            user?.Email,
            ticket.Source,
            ticket.AppVersion,
            ticket.Messages
                .OrderBy(m => m.CreatedAt)
                .Select(m => FeedbackMapping.ToDto(m, baseUrl))
                .ToList());

        if (ticket.HasUnreadForTeam)
        {
            ticket.HasUnreadForTeam = false;
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return new GetAdminFeedbackTicketDetailResponse(dto);
    }
}
