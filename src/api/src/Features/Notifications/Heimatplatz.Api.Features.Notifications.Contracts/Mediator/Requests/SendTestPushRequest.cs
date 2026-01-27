using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Notifications.Contracts.Mediator.Requests;

/// <summary>
/// Request to send a test push notification (Debug only)
/// </summary>
/// <param name="Title">Notification title</param>
/// <param name="Body">Notification body</param>
public record SendTestPushRequest(
    string Title,
    string Body
) : IRequest<SendTestPushResponse>;

/// <summary>
/// Response after sending test push
/// </summary>
/// <param name="SentCount">Number of notifications sent</param>
/// <param name="Message">Status message</param>
public record SendTestPushResponse(int SentCount, string Message);
