using System.Text;
using System.Text.Json;
using Heimatplatz.Maui.ApiClient.Generated;

namespace Heimatplatz.Maui.Features.Properties.Services;

/// <summary>
/// Bezirks-Stempel der Kartenuebersicht: Treffer-Anzahl am (entzerrten)
/// Schwerpunkt der Bezirks-Pins, plus die Pin-Positionen fuer den
/// Hineinzoom beim Antippen.
/// </summary>
public sealed record MapStamp(
    string Name,
    int Count,
    double Lat,
    double Lon,
    IReadOnlyList<(double Lat, double Lon)> PinPositions);

/// <summary>
/// Baut die GeoJSON-Quellen der nativen Karte aus den map-pins der API -
/// gleiche Logik wie die Web-Faltkarte (PropertyMapPanel): einzelne Pins ab
/// Zoom 9, darunter Bezirks-Stempel am Schwerpunkt. Reine Funktionen ohne
/// UI-Bezug, damit die Gruppierung testbar bleibt.
/// </summary>
internal static class MapPinGeoJson
{
    /// <summary>Web: typeKey() - Land=grund, Foreclosure=zv, alles andere haus.</summary>
    public static string TypeKey(PropertyType? type) => type switch
    {
        PropertyType.Land => "grund",
        PropertyType.Foreclosure => "zv",
        _ => "haus",
    };

    /// <summary>Leere FeatureCollection: blendet eine Quelle aus, ohne Layer abzubauen.</summary>
    public const string EmptyFeatureCollection = """{"type":"FeatureCollection","features":[]}""";

    /// <summary>Einzelner Punkt - Quelle des Punkt-Pins der Lage-Karte (Detail/Editor-Vorschau).</summary>
    public static string BuildSinglePoint(double latitude, double longitude)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            writer.WriteStartObject();
            writer.WriteString("type", "Feature");
            writer.WriteStartObject("geometry");
            writer.WriteString("type", "Point");
            writer.WriteStartArray("coordinates");
            writer.WriteNumberValue(longitude);
            writer.WriteNumberValue(latitude);
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteStartObject("properties");
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Umgebungskreis der Lage-Karte als Polygon-Ring (Web: locationCirclePolygon
    /// in map-style.ts, 48 Segmente) - ungefaehre Lagen zeigen ihn mit 300 m Radius.
    /// </summary>
    public static string BuildLocationCircle(double latitude, double longitude, double radiusMeters)
    {
        const int segments = 48;
        // Meter -> Grad wie im Web: 1 Grad Laenge = 111320 m x cos(Breite), 1 Grad Breite = 110540 m
        var lonScale = 111_320 * Math.Cos(latitude * Math.PI / 180);

        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            writer.WriteStartObject();
            writer.WriteString("type", "Feature");
            writer.WriteStartObject("geometry");
            writer.WriteString("type", "Polygon");
            writer.WriteStartArray("coordinates");
            writer.WriteStartArray();
            for (var i = 0; i <= segments; i++)
            {
                var theta = (double)(i % segments) / segments * 2 * Math.PI;
                writer.WriteStartArray();
                writer.WriteNumberValue(longitude + radiusMeters * Math.Cos(theta) / lonScale);
                writer.WriteNumberValue(latitude + radiusMeters * Math.Sin(theta) / 110_540);
                writer.WriteEndArray();
            }
            writer.WriteEndArray();
            writer.WriteEndArray();
            writer.WriteEndObject();
            writer.WriteStartObject("properties");
            writer.WriteEndObject();
            writer.WriteEndObject();
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>FeatureCollection der Einzel-Pins (exact ODER approx, je nach Filter des Aufrufers).</summary>
    public static string BuildPinFeatureCollection(IEnumerable<PropertyMapPinDto> pins)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            foreach (var pin in pins)
            {
                writer.WriteStartObject();
                writer.WriteString("type", "Feature");
                writer.WriteStartObject("geometry");
                writer.WriteString("type", "Point");
                writer.WriteStartArray("coordinates");
                writer.WriteNumberValue(pin.Longitude);
                writer.WriteNumberValue(pin.Latitude);
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteStartObject("properties");
                writer.WriteString("id", pin.Id.ToString("D"));
                writer.WriteString("typ", TypeKey(pin.Type));
                writer.WriteBoolean("exact", !pin.IsApproximate);
                writer.WriteString("preis", PropertyDisplay.Price((decimal)pin.Price));
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    /// <summary>
    /// Pins nach Bezirk gruppieren (Schwerpunkt + Anzahl) und die Schwerpunkte
    /// in Pixeln entzerren - Stadt Linz / Linz-Land liegen sonst uebereinander
    /// (Web: positionClusters). Anders als im Web laeuft die Entzerrung einmalig
    /// auf der Uebersichts-Zoomstufe: die Stempel sind nur bis Zoom 9 sichtbar,
    /// die Abweichung bei anderen Zoomstufen bleibt unsichtbar klein.
    /// </summary>
    /// <param name="overviewZoom">
    /// Tatsaechliche Zoomstufe des Uebersichts-Blicks. MUSS uebergeben werden,
    /// wenn sie vom Default abweicht: die Entzerrung rechnet in Bildschirm-
    /// Pixeln, ein zu hoher Referenzwert schiebt die Stempel zu wenig
    /// auseinander und sie ueberlappen (Hochformat-Handy, 28.07.2026).
    /// </param>
    public static IReadOnlyList<MapStamp> GroupIntoStamps(
        IEnumerable<PropertyMapPinDto> pins,
        IReadOnlyDictionary<Guid, string> districtByMunicipality,
        string otherRegionName,
        double overviewZoom = DefaultOverviewZoom)
    {
        var groups = new Dictionary<string, List<PropertyMapPinDto>>();
        foreach (var pin in pins)
        {
            var name = districtByMunicipality.TryGetValue(pin.MunicipalityId, out var district)
                ? district
                : otherRegionName;
            if (!groups.TryGetValue(name, out var list))
                groups[name] = list = [];
            list.Add(pin);
        }

        var stamps = groups
            .OrderByDescending(g => g.Value.Count)
            .Select(g => new MapStamp(
                g.Key,
                g.Value.Count,
                g.Value.Average(p => p.Latitude),
                g.Value.Average(p => p.Longitude),
                g.Value.Select(p => (p.Latitude, p.Longitude)).ToList()))
            .ToList();

        return Declutter(stamps, overviewZoom);
    }

    public static string BuildStampFeatureCollection(IReadOnlyList<MapStamp> stamps)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("type", "FeatureCollection");
            writer.WriteStartArray("features");
            foreach (var stamp in stamps)
            {
                writer.WriteStartObject();
                writer.WriteString("type", "Feature");
                writer.WriteStartObject("geometry");
                writer.WriteString("type", "Point");
                writer.WriteStartArray("coordinates");
                writer.WriteNumberValue(stamp.Lon);
                writer.WriteNumberValue(stamp.Lat);
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.WriteStartObject("properties");
                writer.WriteString("name", stamp.Name);
                writer.WriteNumber("count", stamp.Count);
                writer.WriteEndObject();
                writer.WriteEndObject();
            }
            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    // Entzerrung wie im Web: 3 Iterationen auf der Uebersichts-Zoomstufe der
    // frisch eingepassten Karte projiziert. Mindestabstand 52px: volle Stempel
    // (Count >= 20) sind 48px + Strich gross - mit den frueheren 46px kuessten
    // sich benachbarte Grossstempel bei vielen Eintraegen (Stresstest 28.07).
    /// <summary>Nur Rueckfall - der echte Wert kommt vom Kamera-Fit der Seite.</summary>
    public const double DefaultOverviewZoom = 7.3;
    private const double MinDistancePixels = 52;

    private static List<MapStamp> Declutter(List<MapStamp> stamps, double overviewZoom)
    {
        if (stamps.Count < 2)
            return stamps;

        var projected = stamps.Select(s => Project(s.Lat, s.Lon, overviewZoom)).ToArray();
        for (var iteration = 0; iteration < 3; iteration++)
        {
            for (var i = 0; i < projected.Length; i++)
            {
                for (var j = i + 1; j < projected.Length; j++)
                {
                    var dx = projected[j].X - projected[i].X;
                    var dy = projected[j].Y - projected[i].Y;
                    var distance = Math.Sqrt(dx * dx + dy * dy);
                    if (distance <= 0 || distance >= MinDistancePixels)
                        continue;

                    var push = (MinDistancePixels - distance) / 2;
                    projected[i] = (projected[i].X - dx / distance * push, projected[i].Y - dy / distance * push);
                    projected[j] = (projected[j].X + dx / distance * push, projected[j].Y + dy / distance * push);
                }
            }
        }

        return stamps
            .Select((stamp, index) =>
            {
                var (lat, lon) = Unproject(projected[index].X, projected[index].Y, overviewZoom);
                return stamp with { Lat = lat, Lon = lon };
            })
            .ToList();
    }

    private static (double X, double Y) Project(double lat, double lon, double zoom)
    {
        var worldSize = 512 * Math.Pow(2, zoom);
        var x = (lon + 180) / 360 * worldSize;
        var latRad = lat * Math.PI / 180;
        var y = (1 - Math.Log(Math.Tan(latRad) + 1 / Math.Cos(latRad)) / Math.PI) / 2 * worldSize;
        return (x, y);
    }

    private static (double Lat, double Lon) Unproject(double x, double y, double zoom)
    {
        var worldSize = 512 * Math.Pow(2, zoom);
        var lon = x / worldSize * 360 - 180;
        var n = Math.PI - 2 * Math.PI * y / worldSize;
        var lat = 180 / Math.PI * Math.Atan(0.5 * (Math.Exp(n) - Math.Exp(-n)));
        return (lat, lon);
    }
}
