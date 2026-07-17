using System.Net.Mail;
using Heimatplatz.Api.Exceptions;

namespace Heimatplatz.Api.Features.Auth.Services;

/// <summary>
/// Zentrale serverseitige Validierung/Normalisierung von Benutzereingaben
/// (Registrierung, Profil-Update, Passwort-Aenderung). Frontends duerfen dieselben
/// Regeln fuer UX-Feedback spiegeln, massgeblich ist aber ausschliesslich diese Logik.
/// </summary>
public static class UserInputValidator
{
    public const int MaxNameLength = 100;
    public const int MaxEmailLength = 256;
    public const int MaxCompanyNameLength = 200;
    public const int MinPasswordLength = 8;
    public const int MaxPasswordLength = 128;

    /// <summary>Normalisiert eine E-Mail-Adresse (Trim + Lowercase) und validiert das Format.</summary>
    public static string NormalizeAndValidateEmail(string? email)
    {
        var normalized = email?.Trim().ToLowerInvariant()
            ?? throw new ValidationException("E-Mail-Adresse ist erforderlich.");

        if (normalized.Length == 0)
            throw new ValidationException("E-Mail-Adresse ist erforderlich.");

        if (normalized.Length > MaxEmailLength)
            throw new ValidationException($"E-Mail-Adresse darf maximal {MaxEmailLength} Zeichen lang sein.");

        // MailAddress akzeptiert auch Display-Name-Formen ("Max <max@x.at>") -
        // deshalb muss die geparste Adresse exakt der Eingabe entsprechen.
        if (!MailAddress.TryCreate(normalized, out var parsed) || parsed.Address != normalized)
            throw new ValidationException("E-Mail-Adresse hat kein gueltiges Format.");

        return normalized;
    }

    /// <summary>Trimmt und validiert Vor-/Nachname.</summary>
    public static string ValidateName(string? value, string fieldLabel)
    {
        var trimmed = value?.Trim();

        if (string.IsNullOrEmpty(trimmed))
            throw new ValidationException($"{fieldLabel} ist erforderlich.");

        if (trimmed.Length > MaxNameLength)
            throw new ValidationException($"{fieldLabel} darf maximal {MaxNameLength} Zeichen lang sein.");

        return trimmed;
    }

    /// <summary>Validiert die Passwort-Richtlinie (min. 8 Zeichen).</summary>
    public static string ValidatePassword(string? password)
    {
        if (string.IsNullOrEmpty(password))
            throw new ValidationException("Passwort ist erforderlich.");

        if (password.Length < MinPasswordLength)
            throw new ValidationException($"Das Passwort muss mindestens {MinPasswordLength} Zeichen lang sein.");

        if (password.Length > MaxPasswordLength)
            throw new ValidationException($"Das Passwort darf maximal {MaxPasswordLength} Zeichen lang sein.");

        return password;
    }

    /// <summary>
    /// Validiert und normalisiert die Verkaeufer-Angaben:
    /// Broker und PropertyManager brauchen einen Firmennamen, Private/kein Verkaeufer nicht.
    /// Gibt (SellerType, CompanyName) in normalisierter Form zurueck.
    /// </summary>
    public static (SellerType? SellerType, string? CompanyName) ValidateSellerInfo(
        SellerType? sellerType,
        string? companyName)
    {
        if (sellerType == null)
            return (null, null);

        if (!Enum.IsDefined(sellerType.Value))
            throw new ValidationException("Unbekannter Anbietertyp.");

        if (sellerType is SellerType.Broker or SellerType.PropertyManager)
        {
            var trimmed = companyName?.Trim();

            if (string.IsNullOrEmpty(trimmed))
                throw new ValidationException("Als Makler/Agentur oder Hausverwaltung muss ein Firmenname angegeben werden.");

            if (trimmed.Length > MaxCompanyNameLength)
                throw new ValidationException($"Der Firmenname darf maximal {MaxCompanyNameLength} Zeichen lang sein.");

            return (sellerType, trimmed);
        }

        // Privatperson: kein Firmenname
        return (sellerType, null);
    }
}
