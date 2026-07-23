using Heimatplatz.Api;
using Heimatplatz.Api.Core.Data;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Contracts.Models;
using Heimatplatz.Api.Features.Feedback.Data.Entities;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Heimatplatz.Api.Features.Feedback.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Erstellt eine neue Feedback-Anfrage mit der ersten Nachricht des Nutzers.
/// Anhaenge muessen zuvor via UploadFeedbackAttachment hochgeladen worden sein.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/feedback")]
public class CreateFeedbackTicketHandler(
    AppDbContext dbContext,
    IFeedbackAttachmentService attachmentService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<CreateFeedbackTicketRequest, CreateFeedbackTicketResponse>
{
    [MediatorHttpPost("", OperationId = "CreateFeedbackTicket", RequiresAuthorization = true)]
    public async Task<CreateFeedbackTicketResponse> Handle(
        CreateFeedbackTicketRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var userId = httpContextAccessor.GetRequiredUserId();

        FeedbackValidation.ValidateMessageInput(request.Body, request.Attachments);
        var attachments = FeedbackValidation.BuildAttachmentEntities(attachmentService, request.Attachments);

        var body = request.Body?.Trim() ?? string.Empty;
        var now = DateTimeOffset.UtcNow;

        var message = new FeedbackMessage
        {
            Author = FeedbackAuthor.User,
            Body = body,
            Attachments = attachments
        };

        // Fortlaufende Nummer je Nutzer + Kategorie fuer den Auto-Titel (z.B. "Lob 1", "Lob 2")
        var sequenceNumber = await dbContext.Set<FeedbackTicket>()
            .CountAsync(t => t.UserId == userId && t.Category == request.Category, cancellationToken) + 1;

        var appVersion = request.AppVersion?.Trim();
        var ticket = new FeedbackTicket
        {
            UserId = userId,
            Category = request.Category,
            Subject = FeedbackMapping.BuildAutoSubject(request.Category, sequenceNumber),
            Status = FeedbackTicketStatus.Open,
            Source = request.Source,
            AppVersion = string.IsNullOrEmpty(appVersion) ? null : FeedbackMapping.Truncate(appVersion, 50),
            LastMessageAt = now,
            HasUnreadForTeam = true,
            Messages = { message }
        };

        dbContext.Set<FeedbackTicket>().Add(ticket);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new CreateFeedbackTicketResponse(ticket.Id, message.Id);
    }
}
