using System.Globalization;
using System.Xml.Linq;
using Heimatplatz.Api;
using Heimatplatz.Api.Features.OpenImmoImport.Models;
using Heimatplatz.Api.Features.Properties.Contracts;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific;
using Heimatplatz.Api.Features.Properties.Contracts.Models.TypeSpecific.Enums;
using Shiny;

namespace Heimatplatz.Api.Features.OpenImmoImport.Services;

public interface IOpenImmoParser
{
    /// <summary>
    /// Parst eine OpenImmo-XML-Datei (1.2.6/1.2.7). Wirft System.Xml.XmlException bei
    /// nicht wohlgeformtem XML - der Orchestrator behandelt das als fehlgeschlagenen
    /// Lauf ohne Marker-Update (Retry beim naechsten Tick).
    /// </summary>
    OpenImmoParseResult Parse(Stream xmlStream);
}

/// <summary>
/// Lenienter OpenImmo-Parser: keine XSD-Validierung, namespace-agnostisch (Feeds kommen
/// mit und ohne Default-Namespace), fehlende Einzelfelder degradieren zu null statt den
/// Lauf zu killen. Produktfilter direkt beim Parsen: nur KAUF-Objekte der Objektarten
/// haus/grundstueck/land_und_forstwirtschaft mit Preis und SourceId werden Listings -
/// alles andere zaehlt als Skipped (Produktregel: keine Wohnungen, kein Miet-Konzept).
/// </summary>
[Service(ApiService.Lifetime, TryAdd = ApiService.TryAdd)]
public class OpenImmoParser : IOpenImmoParser
{
    /// <summary>Property.Description ist auf 4000 Zeichen begrenzt (PropertyConfiguration).</summary>
    private const int MaxDescriptionLength = 4000;

    /// <summary>Property.Title ist auf 2000 Zeichen begrenzt (PropertyConfiguration).</summary>
    private const int MaxTitleLength = 2000;

    public OpenImmoParseResult Parse(Stream xmlStream)
    {
        var document = XDocument.Load(xmlStream);
        var root = document.Root;
        if (root == null)
            return new OpenImmoParseResult { Warnings = ["Leeres XML-Dokument"] };

        var listings = new List<OpenImmoListing>();
        var deletedSourceIds = new List<string>();
        var warnings = new List<string>();
        var skipped = 0;

        // uebertragung umfang="VOLL|TEIL" - TEIL deaktiviert den Snapshot-Delete-Pass
        var uebertragung = El(root, "uebertragung");
        var isPartial = string.Equals(
            uebertragung?.Attribute("umfang")?.Value, "TEIL", StringComparison.OrdinalIgnoreCase);

        foreach (var anbieter in Els(root, "anbieter"))
        {
            foreach (var immobilie in Els(anbieter, "immobilie"))
            {
                var verwaltungTechn = El(immobilie, "verwaltung_techn");
                var sourceId = FirstNonEmpty(
                    Val(verwaltungTechn, "openimmo_obid"),
                    Val(verwaltungTechn, "objektnr_extern"));

                if (string.IsNullOrWhiteSpace(sourceId))
                {
                    skipped++;
                    warnings.Add("Objekt ohne openimmo_obid/objektnr_extern uebersprungen");
                    continue;
                }

                // aktion aktionart="DELETE": explizite Loeschung (nur TEIL-Feeds ueblich)
                var aktionart = El(verwaltungTechn, "aktion")?.Attribute("aktionart")?.Value;
                if (string.Equals(aktionart, "DELETE", StringComparison.OrdinalIgnoreCase))
                {
                    deletedSourceIds.Add(sourceId);
                    continue;
                }

                var listing = TryParseListing(immobilie, verwaltungTechn, sourceId, warnings);
                if (listing == null)
                {
                    skipped++;
                    continue;
                }

                listings.Add(listing);
            }
        }

        return new OpenImmoParseResult
        {
            Listings = listings,
            IsPartialTransfer = isPartial,
            DeletedSourceIds = deletedSourceIds,
            SkippedCount = skipped,
            Warnings = warnings
        };
    }

    private static OpenImmoListing? TryParseListing(
        XElement immobilie, XElement? verwaltungTechn, string sourceId, List<string> warnings)
    {
        var kategorie = El(immobilie, "objektkategorie");

        // Nur Kauf-Objekte: Heimatplatz hat kein Miet-Konzept im Datenmodell
        var vermarktung = El(kategorie, "vermarktungsart");
        if (vermarktung == null || !IsTruthy(vermarktung.Attribute("KAUF")?.Value))
        {
            warnings.Add($"{sourceId}: keine Kauf-Vermarktung - uebersprungen");
            return null;
        }

        var (type, fallbackLabel) = ResolveObjektart(El(kategorie, "objektart"));
        if (type == null)
        {
            warnings.Add($"{sourceId}: Objektart nicht unterstuetzt (nur Haus/Grundstueck) - uebersprungen");
            return null;
        }

        // Preis: ohne Kaufpreis ("auf Anfrage") wird nicht importiert - Preis 0 saehe
        // in Listen/Filtern kaputt aus (Produktentscheidung, siehe Feature-README)
        var preise = El(immobilie, "preise");
        var kaufpreisElement = El(preise, "kaufpreis");
        var price = ParseDecimal(kaufpreisElement?.Value);
        if (IsTruthy(kaufpreisElement?.Attribute("auf_anfrage")?.Value) || price is not > 0)
        {
            warnings.Add($"{sourceId}: kein Kaufpreis (auf Anfrage?) - uebersprungen");
            return null;
        }

        var geo = El(immobilie, "geo");
        var city = Val(geo, "ort") ?? "";
        var street = BuildStreet(geo);

        var verwaltungObjekt = El(immobilie, "verwaltung_objekt");
        var addressReleased = IsTruthy(Val(verwaltungObjekt, "objektadresse_freigeben"));

        var geokoordinaten = El(geo, "geokoordinaten");
        var latitude = ParseDouble(geokoordinaten?.Attribute("breitengrad")?.Value);
        var longitude = ParseDouble(geokoordinaten?.Attribute("laengengrad")?.Value);

        var flaechen = El(immobilie, "flaechen");
        var zustandAngaben = El(immobilie, "zustand_angaben");
        var ausstattung = El(immobilie, "ausstattung");

        var (features, hasGarage, hasGarden, hasBasement) = ParseAusstattung(ausstattung, flaechen);

        var freitexte = El(immobilie, "freitexte");
        var title = BuildTitle(Val(freitexte, "objekttitel"), fallbackLabel, city);
        var description = BuildDescription(freitexte);

        return new OpenImmoListing
        {
            SourceId = sourceId,
            Type = type.Value,
            Title = title,
            Description = description,
            Street = street,
            AddressReleased = addressReleased,
            PostalCode = Val(geo, "plz") ?? "",
            City = city,
            Latitude = latitude,
            Longitude = longitude,
            Price = price.Value,
            LivingAreaSquareMeters = RoundToInt(ParseDecimal(Val(flaechen, "wohnflaeche"))),
            PlotAreaSquareMeters = RoundToInt(ParseDecimal(Val(flaechen, "grundstuecksflaeche"))),
            Rooms = RoundToInt(ParseDecimal(Val(flaechen, "anzahl_zimmer"))),
            YearBuilt = ParseInt(Val(zustandAngaben, "baujahr")),
            Condition = ParseCondition(El(zustandAngaben, "zustand")?.Attribute("zustand_art")?.Value),
            Features = features,
            HasGarage = hasGarage,
            HasGarden = hasGarden,
            HasBasement = hasBasement,
            Bedrooms = RoundToInt(ParseDecimal(Val(flaechen, "anzahl_schlafzimmer"))),
            Bathrooms = RoundToInt(ParseDecimal(Val(flaechen, "anzahl_badezimmer"))),
            Floors = ParseInt(Val(geo, "anzahl_etagen")),
            Zoning = type == PropertyType.Land ? ParseZoning(El(kategorie, "objektart")) : null,
            Contact = ParseContact(El(immobilie, "kontaktperson")),
            Attachments = ParseAttachments(El(immobilie, "anhaenge")),
            StandVom = ParseDateTimeOffset(Val(verwaltungTechn, "stand_vom"))
        };
    }

    /// <summary>
    /// objektart-Mapping: haus → House, grundstueck/land_und_forstwirtschaft → Land,
    /// alles andere (wohnung, buero_praxen, ...) → null = ueberspringen.
    /// Liefert zusaetzlich ein Label fuer den Fallback-Titel.
    /// </summary>
    private static (PropertyType? Type, string Label) ResolveObjektart(XElement? objektart)
    {
        if (objektart == null)
            return (null, "");

        var haus = El(objektart, "haus");
        if (haus != null)
        {
            var haustyp = haus.Attribute("haustyp")?.Value?.ToUpperInvariant();
            var label = haustyp switch
            {
                "EINFAMILIENHAUS" => "Einfamilienhaus",
                "ZWEIFAMILIENHAUS" => "Zweifamilienhaus",
                "MEHRFAMILIENHAUS" => "Mehrfamilienhaus",
                "DOPPELHAUSHAELFTE" => "Doppelhaushälfte",
                "REIHENHAUS" or "REIHENECK" or "REIHENMITTELHAUS" or "REIHENENDHAUS" => "Reihenhaus",
                "BUNGALOW" => "Bungalow",
                "VILLA" => "Villa",
                "BAUERNHAUS" => "Bauernhaus",
                "LANDHAUS" => "Landhaus",
                _ => "Haus"
            };
            return (PropertyType.House, label);
        }

        if (El(objektart, "grundstueck") != null || El(objektart, "land_und_forstwirtschaft") != null)
            return (PropertyType.Land, "Grundstück");

        return (null, "");
    }

    /// <summary>grundstueck@grundst_typ → ZoningType (Default Residential).</summary>
    private static ZoningType ParseZoning(XElement? objektart)
    {
        var typ = El(objektart, "grundstueck")?.Attribute("grundst_typ")?.Value?.ToUpperInvariant();
        if (typ == null && El(objektart, "land_und_forstwirtschaft") != null)
            return ZoningType.Agricultural;

        return typ switch
        {
            "GEWERBE" => ZoningType.Commercial,
            "INDUSTRIE" => ZoningType.Industrial,
            "LAND_FORSTWIRSCHAFT" or "LAND_FORSTWIRTSCHAFT" => ZoningType.Agricultural,
            "GEMISCHT" => ZoningType.Mixed,
            _ => ZoningType.Residential
        };
    }

    private static string? BuildStreet(XElement? geo)
    {
        var strasse = Val(geo, "strasse");
        if (string.IsNullOrWhiteSpace(strasse))
            return null;

        var hausnummer = Val(geo, "hausnummer");
        return string.IsNullOrWhiteSpace(hausnummer) ? strasse.Trim() : $"{strasse.Trim()} {hausnummer.Trim()}";
    }

    private static string BuildTitle(string? objekttitel, string fallbackLabel, string city)
    {
        var title = objekttitel?.Trim();
        if (string.IsNullOrWhiteSpace(title))
            title = string.IsNullOrWhiteSpace(city) ? fallbackLabel : $"{fallbackLabel} in {city}";

        return title.Length > MaxTitleLength ? title[..MaxTitleLength] : title;
    }

    /// <summary>objektbeschreibung + lage + ausstatt_beschr, mit Leerzeilen verbunden, auf 4000 gekappt.</summary>
    private static string? BuildDescription(XElement? freitexte)
    {
        string?[] parts =
        [
            Val(freitexte, "objektbeschreibung"),
            Val(freitexte, "lage"),
            Val(freitexte, "ausstatt_beschr")
        ];

        var text = string.Join("\n\n", parts
            .Select(p => p?.Trim())
            .Where(p => !string.IsNullOrWhiteSpace(p)));

        if (string.IsNullOrWhiteSpace(text))
            return null;

        return text.Length > MaxDescriptionLength ? text[..MaxDescriptionLength] : text;
    }

    private static PropertyCondition? ParseCondition(string? zustandArt)
    {
        if (string.IsNullOrWhiteSpace(zustandArt))
            return null;

        return zustandArt.ToUpperInvariant() switch
        {
            "ERSTBEZUG" or "NEUWERTIG" or "NEUBAU" => PropertyCondition.LikeNew,
            "GEPFLEGT" or "MODERNISIERT" or "VOLL_SANIERT" or "TEIL_SANIERT"
                or "VOLL_RENOVIERT" or "TEIL_RENOVIERT" => PropertyCondition.Good,
            "RENOVIERUNGSBEDUERFTIG" or "ENTKERNT" or "ABRISSOBJEKT" or "BAUFAELLIG"
                => PropertyCondition.NeedsRenovation,
            _ => PropertyCondition.Average
        };
    }

    /// <summary>
    /// Kleine, bewusst unvollstaendige Ausstattungs-Tabelle: nur Merkmale, die die App
    /// als Feature-Chips kennt. OpenImmo-Producer variieren zwischen Element-Werten
    /// ("true") und Attribut-Flags (unterkellert@keller="JA") - beides tolerieren.
    /// </summary>
    private static (List<string> Features, bool HasGarage, bool HasGarden, bool HasBasement)
        ParseAusstattung(XElement? ausstattung, XElement? flaechen)
    {
        var features = new List<string>();
        var hasGarage = false;
        var hasGarden = false;
        var hasBasement = false;

        var unterkellert = El(ausstattung, "unterkellert");
        if (IsTruthy(unterkellert?.Attribute("keller")?.Value) || IsTruthyElement(El(ausstattung, "keller")))
        {
            hasBasement = true;
            features.Add("Keller");
        }

        var stellplatzart = El(ausstattung, "stellplatzart");
        if (stellplatzart != null)
        {
            if (IsTruthy(stellplatzart.Attribute("GARAGE")?.Value) ||
                IsTruthy(stellplatzart.Attribute("TIEFGARAGE")?.Value))
            {
                hasGarage = true;
                features.Add("Garage");
            }

            if (IsTruthy(stellplatzart.Attribute("CARPORT")?.Value))
                features.Add("Carport");
        }

        if (IsTruthyElement(El(ausstattung, "gartennutzung")) ||
            ParseDecimal(Val(flaechen, "gartenflaeche")) > 0)
        {
            hasGarden = true;
            features.Add("Garten");
        }

        if (ParseDecimal(Val(flaechen, "anzahl_balkone")) > 0)
            features.Add("Balkon");
        if (ParseDecimal(Val(flaechen, "anzahl_terrassen")) > 0)
            features.Add("Terrasse");

        if (IsTruthyElement(El(ausstattung, "kamin")))
            features.Add("Kamin");
        if (IsTruthyElement(El(ausstattung, "sauna")))
            features.Add("Sauna");
        if (IsTruthyElement(El(ausstattung, "swimmingpool")))
            features.Add("Pool");
        if (IsTruthyElement(El(ausstattung, "wintergarten")))
            features.Add("Wintergarten");

        return (features, hasGarage, hasGarden, hasBasement);
    }

    private static OpenImmoContact? ParseContact(XElement? kontaktperson)
    {
        if (kontaktperson == null)
            return null;

        var vorname = Val(kontaktperson, "vorname");
        var nachname = Val(kontaktperson, "name");
        var name = string.Join(" ", new[] { vorname, nachname }
            .Select(n => n?.Trim())
            .Where(n => !string.IsNullOrWhiteSpace(n)));
        if (string.IsNullOrWhiteSpace(name))
            name = Val(kontaktperson, "firma") ?? "";

        // email_feedback ist Justimmos Maschinen-Adresse (OpenImmo Feedback XML) - nie anzeigen
        var email = FirstNonEmpty(
            Val(kontaktperson, "email_direkt"),
            Val(kontaktperson, "email_zentrale"));

        var phone = FirstNonEmpty(
            Val(kontaktperson, "tel_durchw"),
            Val(kontaktperson, "tel_handy"),
            Val(kontaktperson, "tel_zentrale"));

        if (string.IsNullOrWhiteSpace(name) && email == null && phone == null)
            return null;

        return new OpenImmoContact
        {
            Name = string.IsNullOrWhiteSpace(name) ? null : name,
            Email = email,
            Phone = phone
        };
    }

    /// <summary>
    /// Bild-Anhaenge in allen drei Liefermodi (EXTERN/REMOTE-URL, Base64, ZIP-Entry).
    /// Nicht-Bilder (Dokumente, Links, Anbieterlogos, Filme) werden verworfen,
    /// TITELBILD-Anhaenge nach vorne sortiert.
    /// </summary>
    private static List<OpenImmoAttachment> ParseAttachments(XElement? anhaenge)
    {
        if (anhaenge == null)
            return [];

        var attachments = new List<OpenImmoAttachment>();

        foreach (var anhang in Els(anhaenge, "anhang"))
        {
            var gruppe = anhang.Attribute("gruppe")?.Value?.ToUpperInvariant();
            if (gruppe is "DOKUMENTE" or "LINKS" or "ANBIETERLOGO" or "FILM" or "FILMLINK" or "QRCODE")
                continue;

            var daten = El(anhang, "daten");
            var pfad = Val(daten, "pfad")?.Trim();
            var base64 = Val(daten, "anhanginhalt")?.Trim();
            var format = Val(anhang, "format");
            var isTitleImage = gruppe == "TITELBILD";

            if (!string.IsNullOrWhiteSpace(base64))
            {
                if (!LooksLikeImage(format, pfad, assumeImageWhenUnknown: true))
                    continue;

                attachments.Add(new OpenImmoAttachment
                {
                    Mode = OpenImmoAttachmentMode.Base64,
                    Base64Content = base64,
                    IsTitleImage = isTitleImage
                });
                continue;
            }

            if (string.IsNullOrWhiteSpace(pfad))
                continue;

            if (!LooksLikeImage(format, pfad, assumeImageWhenUnknown: false))
                continue;

            var location = anhang.Attribute("location")?.Value?.ToUpperInvariant();
            var isExternal = location is "EXTERN" or "REMOTE"
                || pfad.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || pfad.StartsWith("https://", StringComparison.OrdinalIgnoreCase);

            attachments.Add(new OpenImmoAttachment
            {
                Mode = isExternal ? OpenImmoAttachmentMode.ExternalUrl : OpenImmoAttachmentMode.ZipEntry,
                Location = pfad,
                IsTitleImage = isTitleImage
            });
        }

        return attachments
            .OrderByDescending(a => a.IsTitleImage)
            .ToList();
    }

    private static bool LooksLikeImage(string? format, string? pfad, bool assumeImageWhenUnknown)
    {
        string[] imageMarkers = ["jpg", "jpeg", "png", "webp"];

        if (!string.IsNullOrWhiteSpace(format))
        {
            var formatLower = format.ToLowerInvariant();
            if (imageMarkers.Any(formatLower.Contains))
                return true;
            // Explizit anderes Format (pdf, mp4, ...) - kein Bild
            return false;
        }

        if (!string.IsNullOrWhiteSpace(pfad))
        {
            var pathPart = pfad.Split('?', 2)[0].ToLowerInvariant();
            if (imageMarkers.Any(m => pathPart.EndsWith("." + m, StringComparison.Ordinal)))
                return true;
            // URLs ohne Extension (CDN): erst der Download klaert den Content-Type
            return !Path.HasExtension(pathPart) || assumeImageWhenUnknown;
        }

        return assumeImageWhenUnknown;
    }

    // === Namespace-agnostische XML-Helfer (Feeds kommen mit und ohne Default-Namespace) ===

    private static XElement? El(XElement? parent, string localName)
        => parent?.Elements().FirstOrDefault(e => e.Name.LocalName == localName);

    private static IEnumerable<XElement> Els(XElement? parent, string localName)
        => parent?.Elements().Where(e => e.Name.LocalName == localName) ?? [];

    private static string? Val(XElement? parent, string localName)
    {
        var value = El(parent, localName)?.Value;
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static string? FirstNonEmpty(params string?[] values)
        => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v))?.Trim();

    /// <summary>OpenImmo-Booleans variieren: true/1/JA (Attribute) bzw. Element-Werte.</summary>
    private static bool IsTruthy(string? value)
        => value != null && value.Trim().ToUpperInvariant() is "TRUE" or "1" or "JA" or "YES";

    /// <summary>Leeres Element = Merkmal vorhanden; sonst entscheidet der Wert.</summary>
    private static bool IsTruthyElement(XElement? element)
    {
        if (element == null)
            return false;
        return string.IsNullOrWhiteSpace(element.Value) || IsTruthy(element.Value);
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        // Bewusst OHNE AllowThousands: .NET validiert Gruppenpositionen nicht, ein
        // deutsches "350000,75" wuerde invariant sonst als 35000075 durchgehen.
        // OpenImmo schreibt Dezimalpunkt vor; ein einzelnes Komma ohne Punkt wird
        // als deutsches Dezimalkomma toleriert.
        value = value.Trim();
        if (value.Contains(',') && !value.Contains('.'))
            value = value.Replace(',', '.');

        return decimal.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }

    private static double? ParseDouble(string? value)
        => (double?)ParseDecimal(value);

    private static int? ParseInt(string? value)
        => RoundToInt(ParseDecimal(value));

    private static int? RoundToInt(decimal? value)
        => value.HasValue ? (int)Math.Round(value.Value, MidpointRounding.AwayFromZero) : null;

    private static DateTimeOffset? ParseDateTimeOffset(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParse(
            value.Trim(), CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal, out var result)
            ? result
            : null;
    }
}
