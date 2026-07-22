namespace Heimatplatz.Api.Features.Legal.Data;

/// <summary>
/// Firmen-Stammdaten fuer den ERST-Seed einer leeren Datenbank - keine Laufzeit-Quelle!
///
/// Die Wahrheit sind die LegalSettings-Datensaetze in der Datenbank; geaendert wird
/// ausschliesslich ueber /intern/kontakt (POST /api/admin/legal/*). Diese Konstanten
/// existieren nur, damit eine frische DB (lokal, Test, Neuaufsetzen) ueberhaupt mit
/// gueltigen Pflichtangaben startet - und damit es dafuer genau EINE Stelle gibt.
///
/// Frueher lagen dieselben Werte zusaetzlich im GetPrivacyPolicyHandler (On-Demand-Seed)
/// und in der Migration 20260403000000_SeedLegalSettings; die Kopien sind auseinander-
/// gelaufen (Telefonnummer mal gesetzt, mal null). Nicht wieder duplizieren.
/// </summary>
internal static class CompanyMasterData
{
    public const string CompanyName = "Ing. Daniel Hufnagl";
    public const string LegalForm = "Einzelunternehmen";
    public const string Owner = "Ing. Daniel Hufnagl";

    public const string Street = "Stockham 44";
    public const string PostalCode = "4663";
    public const string City = "Laakirchen";
    public const string Country = "Österreich";

    public const string Email = "info@heimatplatz.at";
    public const string Website = "https://www.heimatplatz.at";

    /// <summary>
    /// Telefonnummer in Anzeigeform (der tel:-Link wird daraus berechnet, siehe
    /// <see cref="Services.PhoneNumberFormatter"/>). Leerstring ist zulaessig - alle
    /// Frontends blenden die Telefonzeile dann aus.
    /// </summary>
    public const string Phone = "+43 664 73221804";

    public const string UidNumber = "ATU75151817";
    public const string TaxNumber = "532163383";
    public const string DunsNumber = "30-080-8592";
    public const string Gln = "9110026231195";
    public const string GisaNumber = "31233118";

    public const string Trade = "Dienstleistungen in der automatischen Datenverarbeitung und Informationstechnik";
    public const string TradeAuthority = "Bezirkshauptmannschaft Gmunden";
    public const string ProfessionalLaw = "Gewerbeordnung 1994 (GewO)";
    public const string ChamberMembership = "Wirtschaftskammer Oberösterreich";
    public const string TradeGroup = "Fachgruppe Unternehmensberatung, Buchhaltung und Informationstechnologie";
}
