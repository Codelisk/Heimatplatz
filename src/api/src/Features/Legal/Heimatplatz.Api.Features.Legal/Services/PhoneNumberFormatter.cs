namespace Heimatplatz.Api.Features.Legal.Services;

/// <summary>
/// Baut aus der Anzeige-Telefonnummer den Wert fuer href="tel:...".
///
/// Bewusst konservativ: nur Trennzeichen entfernen und 00 zu + normalisieren. Kein Erraten
/// einer Landesvorwahl bei fuehrender 0 - eine falsch geratene Vorwahl waehlt beim Nutzer
/// eine fremde Nummer. Gepflegt wird deshalb immer international ("+43 ...").
/// </summary>
public static class PhoneNumberFormatter
{
    public static string? ToTelLink(string? phone)
    {
        if (string.IsNullOrWhiteSpace(phone))
            return null;

        // "(0)" ist die optionale nationale Verkehrsausscheidungsziffer ("+43 (0)664 ..."):
        // beim internationalen Waehlen MUSS sie entfallen, sonst waehlt der Link eine
        // falsche Nummer. In Klammern steht sie ausschliesslich in dieser Bedeutung.
        var trimmed = phone.Trim().Replace("(0)", string.Empty);
        var digits = new string([.. trimmed.Where(char.IsAsciiDigit)]);

        if (digits.Length == 0)
            return null;

        if (trimmed.StartsWith('+'))
            return $"+{digits}";

        // Internationale Amtskennzahl (00 43 ...) auf die kompakte +-Form bringen
        if (digits.StartsWith("00") && digits.Length > 2)
            return $"+{digits[2..]}";

        return digits;
    }
}
