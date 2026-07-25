using FluentAssertions;
using Heimatplatz.Api.Features.Properties.Handlers;
using NUnit.Framework;

namespace Heimatplatz.Api.UnitTests.Features.Properties;

/// <summary>
/// Privacy-Jitter der Kartenansicht (GetPropertyMapPinsHandler.ApplyPrivacyJitter):
/// ungenaue Lagen werden deterministisch aus der Property-Id gestreut - gleiche
/// Immobilie ergibt immer denselben Punkt (kein Springen zwischen Requests),
/// aber nie die exakte Hausanschrift.
/// </summary>
[TestFixture]
public class PropertyMapPinJitterTests
{
    // Linz-Zentrum als realistischer Ausgangspunkt
    private const double Latitude = 48.3069;
    private const double Longitude = 14.2858;

    [Test]
    public void ApplyPrivacyJitter_IsDeterministicPerId()
    {
        var id = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var first = GetPropertyMapPinsHandler.ApplyPrivacyJitter(id, Latitude, Longitude);
        var second = GetPropertyMapPinsHandler.ApplyPrivacyJitter(id, Latitude, Longitude);

        second.Latitude.Should().Be(first.Latitude);
        second.Longitude.Should().Be(first.Longitude);
    }

    [Test]
    public void ApplyPrivacyJitter_NeverReturnsTheOriginalPosition()
    {
        // Mehrere feste Ids: keine darf auf dem Original landen (Mindestradius 150 m)
        foreach (var seed in new[] { "a", "b", "c", "d", "e" })
        {
            var id = DeterministicGuid(seed);
            var (latitude, longitude) = GetPropertyMapPinsHandler.ApplyPrivacyJitter(id, Latitude, Longitude);
            (latitude != Latitude || longitude != Longitude).Should().BeTrue(
                $"Id-Seed '{seed}' darf nicht auf der exakten Anschrift liegen");
        }
    }

    [Test]
    public void ApplyPrivacyJitter_StaysWithinExpectedDistanceBand()
    {
        // Konstruktionsbedingt 150-399 m Versatz; mit Toleranz pruefen
        foreach (var seed in new[] { "haus", "grund", "zv", "linz", "gmunden", "wels", "steyr", "enns" })
        {
            var id = DeterministicGuid(seed);
            var (latitude, longitude) = GetPropertyMapPinsHandler.ApplyPrivacyJitter(id, Latitude, Longitude);

            var meters = DistanceInMeters(Latitude, Longitude, latitude, longitude);
            meters.Should().BeInRange(140, 410, $"Id-Seed '{seed}' muss im Streuband liegen");
        }
    }

    [Test]
    public void ApplyPrivacyJitter_DifferentIdsSpreadDifferently()
    {
        var first = GetPropertyMapPinsHandler.ApplyPrivacyJitter(DeterministicGuid("erste"), Latitude, Longitude);
        var second = GetPropertyMapPinsHandler.ApplyPrivacyJitter(DeterministicGuid("zweite"), Latitude, Longitude);

        // Sonst wuerden alle Inserate eines Orts wieder aufeinander liegen
        (first.Latitude != second.Latitude || first.Longitude != second.Longitude).Should().BeTrue();
    }

    /// <summary>Stabile Guid aus einem Seed-String (Tests bleiben deterministisch).</summary>
    private static Guid DeterministicGuid(string seed)
    {
        var bytes = new byte[16];
        var seedBytes = System.Text.Encoding.UTF8.GetBytes(seed);
        for (var i = 0; i < bytes.Length; i++)
            bytes[i] = (byte)(seedBytes[i % seedBytes.Length] + i * 31);
        return new Guid(bytes);
    }

    /// <summary>Plattkarten-Naeherung reicht fuer wenige hundert Meter.</summary>
    private static double DistanceInMeters(double lat1, double lng1, double lat2, double lng2)
    {
        var north = (lat2 - lat1) * 111_320.0;
        var east = (lng2 - lng1) * 111_320.0 * Math.Cos(lat1 * Math.PI / 180.0);
        return Math.Sqrt(north * north + east * east);
    }
}
