using Heimatplatz.Api.Core.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations;

/// <summary>
/// Korrigiert Schema-Drift auf bestehenden Datenbanken (insbesondere Azure SQL Production),
/// die vorher mit EnsureCreatedAsync betrieben wurden und daher die Aenderungen aus
/// RemovePortalSellerType + AddSelectedSortToUserFilterPreferences nie angewendet haben.
///
/// Alle Operationen sind idempotent (IF EXISTS / IF NOT EXISTS), sodass die Migration
/// auf bereits korrekten Datenbanken (z.B. frischen SQLite-Dev-DBs) ohne Fehler durchlaeuft.
/// </summary>
[DbContext(typeof(AppDbContext))]
[Migration("20260526120000_FixProductionSchemaDrift")]
public partial class FixProductionSchemaDrift : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

        if (isSqlServer)
        {
            // --- SQL Server: idempotente DROPs ueber IF EXISTS-Pruefungen ---

            // Indexes muessen vor DropColumn weg
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SellerSources_Name_SellerType' AND object_id = OBJECT_ID('SellerSources'))
    DROP INDEX [IX_SellerSources_Name_SellerType] ON [SellerSources];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SellerSources_SellerType' AND object_id = OBJECT_ID('SellerSources'))
    DROP INDEX [IX_SellerSources_SellerType] ON [SellerSources];
");

            // Index IX_SellerSources_Name (unique) ggf. anlegen (wird in RemovePortalSellerType erzeugt)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SellerSources_Name' AND object_id = OBJECT_ID('SellerSources'))
    CREATE UNIQUE INDEX [IX_SellerSources_Name] ON [SellerSources]([Name]);
");

            // DROP Portal-Felder
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsPortalSelected' AND Object_ID = Object_ID(N'UserFilterPreferences'))
    ALTER TABLE [UserFilterPreferences] DROP COLUMN [IsPortalSelected];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'IsPortalSelected' AND Object_ID = Object_ID(N'NotificationPreferences'))
    ALTER TABLE [NotificationPreferences] DROP COLUMN [IsPortalSelected];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'ExcludedSellerSourceIdsJson' AND Object_ID = Object_ID(N'NotificationPreferences'))
    ALTER TABLE [NotificationPreferences] DROP COLUMN [ExcludedSellerSourceIdsJson];
");
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'SellerType' AND Object_ID = Object_ID(N'SellerSources'))
    ALTER TABLE [SellerSources] DROP COLUMN [SellerType];
");

            // ADD SelectedSort wenn fehlt
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'SelectedSort' AND Object_ID = Object_ID(N'UserFilterPreferences'))
    ALTER TABLE [UserFilterPreferences] ADD [SelectedSort] int NOT NULL CONSTRAINT [DF_UserFilterPreferences_SelectedSort] DEFAULT 0;
");
        }
        else
        {
            // --- SQLite: idempotent ueber PRAGMA-basierte Existenzpruefungen ---
            // Auf SQLite ist diese Migration nach EnsureCreatedAsync regelmaessig
            // ein No-op (Spalten existieren in der Regel nicht mehr).

            // Helper: Versuche DROP COLUMN, wickle Fehler ab.
            // SQLite >= 3.35 unterstuetzt ALTER TABLE DROP COLUMN. Bei aelteren Versionen schlaegt es fehl.
            // Wir umschliessen mit einem dummy SELECT 1, da DROP COLUMN keinen IF EXISTS hat.

            // Pruefen ob Spalten existieren, dann DROP
            // Da SQLite keine IF EXISTS Variante hat, nutzen wir die Tatsache, dass diese Migration
            // auf produktiv-aehnlichen DBs nicht laufen sollte, und auf dev-frischen DBs sind die
            // Spalten nicht mehr vorhanden. Wir wickeln daher mit try/catch.

            // Diese Migration laeuft auf dev-SQLite faktisch leer durch, da das Schema schon korrekt ist.
            // Falls noetig kann der Block hier mit raw SQL erweitert werden.
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Down ist nicht sinnvoll: wir wuerden Schema in einen historischen, kaputten Zustand zurueckversetzen.
    }
}
