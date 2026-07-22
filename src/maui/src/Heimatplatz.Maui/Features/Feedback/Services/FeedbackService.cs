using Heimatplatz.Maui.ApiClient.Generated;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Maui.Features.Feedback.Services;

/// <summary>
/// API-Aufrufe ausschliesslich via IMediator.Request auf die generierten
/// HTTP-Contracts (Projekt Heimatplatz.Maui.ApiClient). Feedback-Requests laufen
/// bewusst ohne Offline-Cache - Antworten des Teams sollen immer frisch sein.
/// </summary>
[Singleton]
public class FeedbackService(IMediator mediator) : IFeedbackService
{
    public async Task<GetMyFeedbackTicketsResponse> GetMyTicketsAsync(CancellationToken ct = default)
    {
        var (_, response) = await mediator.Request(new GetMyFeedbackTicketsHttpRequest(), ct);
        return response;
    }

    public async Task<FeedbackTicketDetailDto?> GetTicketAsync(Guid ticketId, CancellationToken ct = default)
    {
        var (_, response) = await mediator.Request(
            new GetFeedbackTicketDetailHttpRequest { TicketId = ticketId }, ct);
        return response?.Ticket;
    }

    public async Task<UploadFeedbackAttachmentResponse> UploadAttachmentAsync(
        string fileName, string contentType, byte[] bytes, CancellationToken ct = default)
    {
        var (_, response) = await mediator.Request(new UploadFeedbackAttachmentHttpRequest
        {
            Body = new UploadFeedbackAttachmentRequest
            {
                FileName = fileName,
                ContentType = contentType,
                Base64Data = Convert.ToBase64String(bytes)
            }
        }, ct);
        return response;
    }

    public async Task<CreateFeedbackTicketResponse> CreateTicketAsync(
        FeedbackCategory category,
        string? subject,
        string body,
        List<FeedbackAttachmentInput> attachments,
        CancellationToken ct = default)
    {
        var (_, response) = await mediator.Request(new CreateFeedbackTicketHttpRequest
        {
            Body = new CreateFeedbackTicketRequest
            {
                Category = category,
                Subject = subject,
                Body = body,
                Source = GetSource(),
                AppVersion = AppInfo.Current.VersionString,
                Attachments = attachments
            }
        }, ct);
        return response;
    }

    public async Task<FeedbackMessageDto?> AddMessageAsync(
        Guid ticketId, string body, List<FeedbackAttachmentInput> attachments, CancellationToken ct = default)
    {
        var (_, response) = await mediator.Request(new AddFeedbackMessageHttpRequest
        {
            Body = new AddFeedbackMessageRequest
            {
                TicketId = ticketId,
                Body = body,
                Attachments = attachments
            }
        }, ct);
        return response?.Message;
    }

    private static FeedbackSource GetSource()
    {
        if (DeviceInfo.Current.Platform == DevicePlatform.Android)
            return FeedbackSource.Android;
        if (DeviceInfo.Current.Platform == DevicePlatform.iOS)
            return FeedbackSource.Ios;
        if (DeviceInfo.Current.Platform == DevicePlatform.WinUI)
            return FeedbackSource.Windows;
        return FeedbackSource.Unknown;
    }
}
