using Heimatplatz.Api;
using Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;
using Heimatplatz.Api.Features.Feedback.Infrastructure;
using Heimatplatz.Api.Features.Feedback.Services;
using Microsoft.AspNetCore.Http;
using Shiny;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Handlers;

/// <summary>
/// Upload eines einzelnen Feedback-Anhangs (Bild oder Sprachnachricht) als Base64-JSON.
/// Ein Anhang pro Request - gleiches Muster wie der Inserats-Foto-Upload.
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
[MediatorHttpGroup("/api/feedback")]
public class UploadFeedbackAttachmentHandler(
    IFeedbackAttachmentService attachmentService,
    IHttpContextAccessor httpContextAccessor
) : IRequestHandler<UploadFeedbackAttachmentRequest, UploadFeedbackAttachmentResponse>
{
    [MediatorHttpPost("/attachments", OperationId = "UploadFeedbackAttachment", RequiresAuthorization = true)]
    public async Task<UploadFeedbackAttachmentResponse> Handle(
        UploadFeedbackAttachmentRequest request,
        IMediatorContext context,
        CancellationToken cancellationToken)
    {
        var saved = await attachmentService.SaveBase64Async(
            request.FileName, request.ContentType, request.Base64Data, cancellationToken);

        var baseUrl = FeedbackMapping.GetBaseUrl(httpContextAccessor);
        return new UploadFeedbackAttachmentResponse(
            $"{baseUrl}{saved.RelativeUrl}",
            saved.Kind,
            saved.FileSizeBytes);
    }
}
