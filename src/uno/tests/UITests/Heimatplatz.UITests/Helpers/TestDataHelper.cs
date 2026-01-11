namespace Heimatplatz.UITests.Helpers;

/// <summary>
/// Helper fuer Test-Datengenerierung.
/// </summary>
public static class TestDataHelper
{
    private static readonly Random Random = new();

    /// <summary>
    /// Generiert eine zufaellige E-Mail-Adresse.
    /// </summary>
    public static string RandomEmail(string domain = "test.example.com")
    {
        var timestamp = DateTime.Now.Ticks;
        var random = Random.Next(1000, 9999);
        return $"testuser_{timestamp}_{random}@{domain}";
    }

    /// <summary>
    /// Generiert einen zufaelligen Benutzernamen.
    /// </summary>
    public static string RandomUsername(string prefix = "TestUser")
    {
        var timestamp = DateTime.Now.Ticks % 100000;
        var random = Random.Next(100, 999);
        return $"{prefix}_{timestamp}_{random}";
    }

    /// <summary>
    /// Generiert ein zufaelliges Passwort.
    /// </summary>
    public static string RandomPassword(int length = 12)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789!@#$%";
        var password = new char[length];

        for (var i = 0; i < length; i++)
        {
            password[i] = chars[Random.Next(chars.Length)];
        }

        return new string(password);
    }

    /// <summary>
    /// Generiert einen zufaelligen String.
    /// </summary>
    public static string RandomString(int length = 10)
    {
        const string chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new char[length];

        for (var i = 0; i < length; i++)
        {
            result[i] = chars[Random.Next(chars.Length)];
        }

        return new string(result);
    }

    /// <summary>
    /// Generiert eine zufaellige Telefonnummer.
    /// </summary>
    public static string RandomPhoneNumber()
    {
        return $"+49{Random.Next(100, 999)}{Random.Next(1000000, 9999999)}";
    }

    /// <summary>
    /// Generiert ein zufaelliges Datum in der Vergangenheit.
    /// </summary>
    public static DateTime RandomPastDate(int maxDaysAgo = 365)
    {
        var daysAgo = Random.Next(1, maxDaysAgo);
        return DateTime.Now.AddDays(-daysAgo);
    }

    /// <summary>
    /// Generiert ein zufaelliges Datum in der Zukunft.
    /// </summary>
    public static DateTime RandomFutureDate(int maxDaysAhead = 365)
    {
        var daysAhead = Random.Next(1, maxDaysAhead);
        return DateTime.Now.AddDays(daysAhead);
    }

    /// <summary>
    /// Generiert eine zufaellige Zahl.
    /// </summary>
    public static int RandomNumber(int min = 1, int max = 100)
    {
        return Random.Next(min, max + 1);
    }

    /// <summary>
    /// Generiert eine zufaellige GUID.
    /// </summary>
    public static string RandomGuid()
    {
        return Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Standard-Testbenutzer Credentials.
    /// </summary>
    public static class TestUsers
    {
        public const string ValidUsername = "testuser@example.com";
        public const string ValidPassword = "Test123!@#";

        public const string InvalidUsername = "invalid@example.com";
        public const string InvalidPassword = "wrongpassword";

        public const string AdminUsername = "admin@example.com";
        public const string AdminPassword = "Admin123!@#";
    }
}
