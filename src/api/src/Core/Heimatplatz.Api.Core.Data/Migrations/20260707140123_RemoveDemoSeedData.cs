using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <summary>
    /// Entfernt geseedete Demo-/Testdaten aus bestehenden Datenbanken (insb. Produktion):
    /// die Test-Accounts mit bekannten Passwoertern (UserSeeder) samt aller abhaengigen Daten
    /// sowie die Demo-Zwangsversteigerungen (ForeclosureAuctionSeeder).
    /// Reine Daten-Migration ohne Schema-Aenderung, SQL laeuft auf SQLite UND SQL Server.
    /// Idempotent: DELETEs auf bereits entfernte Zeilen sind wirkungslos.
    /// </summary>
    public partial class RemoveDemoSeedData : Migration
    {
        // Seed-User aus UserSeeder (bekannte Test-Passwoerter). Der System-User
        // system@heimatplatz.at bleibt erhalten - er besitzt die echten EDIKTE-Properties.
        private const string SeedUserEmails =
            "'max.mustermann@example.com','anna.schmidt@example.com','thomas.mueller@example.com'," +
            "'lisa.weber@example.com','admin@heimatplatz.de'," +
            "'test.buyer@heimatplatz.dev','test.seller@heimatplatz.dev','test.both@heimatplatz.dev'";

        // GUID-Literale bewusst vermieden (Format-Unterschiede SQLite/SQL Server) -
        // alle Loeschungen sind ueber Subselects an den Seed-User-Emails verankert.
        private const string SeedUserIds =
            "SELECT Id FROM Users WHERE Email IN (" + SeedUserEmails + ")";

        // Demo-Properties des PropertySeeders gehoeren ausnahmslos den Seed-Usern
        private const string DemoPropertyIds =
            "SELECT Id FROM Properties WHERE UserId IN (" + SeedUserIds + ")";

        // Demo-Zwangsversteigerungen des ForeclosureAuctionSeeders: eindeutig erkennbar an
        // ExternalId IS NULL (echte EDIKTE-Eintraege haben immer eine ExternalId) plus Aktenzeichen
        private const string DemoAuctionCaseNumbers =
            "'567 E 890/24','678 E 901/24','234 E 123/24','890 E 456/24'," +
            "'123 E 789/24','456 E 234/24','789 E 567/24','901 E 890/24'";

        private const string DemoAuctionIds =
            "SELECT Id FROM ForeclosureAuctions WHERE ExternalId IS NULL AND CaseNumber IN (" + DemoAuctionCaseNumbers + ")";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Kinder der Demo-Properties zuerst - erfasst auch Favoriten/Blockierungen
            //    ECHTER Nutzer, die auf Demo-Properties zeigen (PropertyId-Subselect)
            migrationBuilder.Sql($"DELETE FROM PropertyContactInfos WHERE PropertyId IN ({DemoPropertyIds})");
            migrationBuilder.Sql($"DELETE FROM Favorites WHERE UserId IN ({SeedUserIds}) OR PropertyId IN ({DemoPropertyIds})");
            migrationBuilder.Sql($"DELETE FROM BlockedProperties WHERE UserId IN ({SeedUserIds}) OR PropertyId IN ({DemoPropertyIds})");

            // 2. Restliche benutzerbezogene Daten der Seed-User.
            //    PushSubscriptions zusaetzlich ueber das Test-Token-Praefix des NotificationsSeeders
            //    (der Seeder hat u.U. auch dem System-User ein Test-Token angelegt).
            migrationBuilder.Sql($"DELETE FROM ListingAnalyses WHERE UserId IN ({SeedUserIds})");
            migrationBuilder.Sql($"DELETE FROM NotificationPreferences WHERE UserId IN ({SeedUserIds}) OR UserId IN (SELECT Id FROM Users WHERE Email = 'system@heimatplatz.at')");
            migrationBuilder.Sql($"DELETE FROM PushSubscriptions WHERE UserId IN ({SeedUserIds}) OR DeviceToken LIKE 'test-device-token-%'");

            // 3. Demo-Properties, dann restliche Auth-Daten, zuletzt die Seed-User selbst
            migrationBuilder.Sql($"DELETE FROM Properties WHERE UserId IN ({SeedUserIds})");
            migrationBuilder.Sql($"DELETE FROM RefreshTokens WHERE UserId IN ({SeedUserIds})");
            migrationBuilder.Sql($"DELETE FROM UserFilterPreferences WHERE UserId IN ({SeedUserIds})");
            migrationBuilder.Sql($"DELETE FROM UserRoles WHERE UserId IN ({SeedUserIds})");
            migrationBuilder.Sql($"DELETE FROM Users WHERE Email IN ({SeedUserEmails})");

            // 4. Demo-Zwangsversteigerungen (Kinder zuerst)
            migrationBuilder.Sql($"DELETE FROM ForeclosureAuctionChanges WHERE ForeclosureAuctionId IN ({DemoAuctionIds})");
            migrationBuilder.Sql($"DELETE FROM ForeclosureAuctions WHERE ExternalId IS NULL AND CaseNumber IN ({DemoAuctionCaseNumbers})");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Bewusst leer: Datenloeschung ist nicht reversibel.
            // Demo-Daten koennen in Development ueber Database:EnableSeeding=true neu erzeugt werden.
        }
    }
}
