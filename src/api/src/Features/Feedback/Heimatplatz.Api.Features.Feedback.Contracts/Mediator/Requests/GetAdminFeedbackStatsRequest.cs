using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Feedback.Contracts.Mediator.Requests;

/// <summary>Intern: Kennzahlen fuer die Dashboard-Karte (offene/ungelesene Anfragen).</summary>
public record GetAdminFeedbackStatsRequest : IRequest<GetAdminFeedbackStatsResponse>;

public record GetAdminFeedbackStatsResponse(
    int Total,
    int Open,
    int InProgress,
    int UnreadFromUser);
