using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Heimatplatz.Api.Features.Feedback.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Weitere Nutzer-Nachricht an einer bestehenden Anfrage. Beantwortete oder
/// geschlossene Anfragen werden dadurch wieder geoeffnet (Team-Badge leuchtet erneut).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/feedback")]
public class AddFeedbackMessageHandler(
    AppDbContext dbContext,
    IFeedbackAttachmentService attachmentService,
    IHttpContextAccessor httpContextAccessor,
    IConfiguration configuration
) : IRequestHandler<AddFeedbackMessageRequest, AddFeedbackMessageResponse>
{
    [MediatorHttpPost("/messages", OperationId = "AddFeedbackMessage", RequiresAuthorization = true)]
    public async Task<AddFeedbackMessageResponse> Handle(
        AddFeedbackMessageRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        var ticket = await dbContext.Set<FeedbackTicket>()
            .FirstOrDefaultAsync(t => t.Id == request.TicketId && t.UserId == userId, cancellationToken);

        if (ticket == null)
            return new AddFeedbackMessageResponse(null);

        FeedbackValidation.ValidateMessageInput(request.Body, request.Attachments);
        var attachments = FeedbackValidation.BuildAttachmentEntities(attachmentService, request.Attachments);

        var message = new FeedbackMessage
        {
            TicketId = ticket.Id,
            Author = FeedbackAuthor.User,
            Body = request.Body?.Trim() ?? string.Empty,
            Attachments = attachments
        };

        dbContext.Set<FeedbackMessage>().Add(message);

        ticket.LastMessageAt = DateTimeOffset.UtcNow;
        ticket.HasUnreadForTeam = true;
        if (ticket.Status is FeedbackTicketStatus.Answered or FeedbackTicketStatus.Closed)
            ticket.Status = FeedbackTicketStatus.Open;

        await dbContext.SaveChangesAsync(cancellationToken);

        var baseUrl = FeedbackMapping.GetBaseUrl(httpContextAccessor, configuration);
        return new AddFeedbackMessageResponse(FeedbackMapping.ToDto(message, baseUrl));
    }
}
