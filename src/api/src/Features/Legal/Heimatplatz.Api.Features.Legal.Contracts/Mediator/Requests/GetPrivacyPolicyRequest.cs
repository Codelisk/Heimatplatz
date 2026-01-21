using Heimatplatz.Api.Features.Legal.Contracts.Models;
using Shiny.Mediator;

namespace Heimatplatz.Api.Features.Legal.Contracts.Mediator.Requests;

/// <summary>
/// Request zum Abrufen der aktuellen Datenschutzerklaerung
/// </summary>
public record GetPrivacyPolicyRequest : IRequest<GetPrivacyPolicyResponse>;

/// <summary>
/// Response mit der Datenschutzerklaerung
/// </summary>
public record GetPrivacyPolicyResponse(PrivacyPolicyDto? PrivacyPolicy);
