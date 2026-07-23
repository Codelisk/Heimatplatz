namespace Heimatplatz.Api.Exceptions;

/// <summary>
/// Basis fuer fachliche Ausnahmen, die einen definierten HTTP-Statuscode an den Client melden.
/// Wird zentral von <c>ApiExceptionHandler</c> behandelt und als ProblemDetails zurueckgegeben.
/// </summary>
public abstract class ApiException(int statusCode, string title, string message) : Exception(message)
{
    /// <summary>Der HTTP-Statuscode, der an den Client gemeldet wird.</summary>
    public int StatusCode { get; } = statusCode;

    /// <summary>Der ProblemDetails-Titel.</summary>
    public string Title { get; } = title;
}

/// <summary>
/// Fachlicher Konflikt (HTTP 409), z. B. eine bereits vergebene E-Mail-Adresse.
/// </summary>
public sealed class ConflictException(string message) : ApiException(409, "Conflict", message);

/// <summary>
/// Ungueltige Eingabe / verletzte Geschaeftsregel (HTTP 400).
/// </summary>
public sealed class ValidationException(string message) : ApiException(400, "Validation Failed", message);

/// <summary>
/// Authentifizierter Benutzer hat nicht die benoetigte Berechtigung (HTTP 403).
/// </summary>
public sealed class ForbiddenException(string message) : ApiException(403, "Forbidden", message);

/// <summary>
/// Ein benoetigter externer Dienst ist gerade nicht erreichbar (HTTP 503),
/// z. B. der SMTP-Server beim Mail-Versand.
/// </summary>
public sealed class ServiceUnavailableException(string message) : ApiException(503, "Service Unavailable", message);

/// <summary>
/// Angeforderte Ressource existiert nicht oder gehoert einem anderen Benutzer (HTTP 404).
/// </summary>
public sealed class NotFoundException(string message) : ApiException(404, "Not Found", message);
