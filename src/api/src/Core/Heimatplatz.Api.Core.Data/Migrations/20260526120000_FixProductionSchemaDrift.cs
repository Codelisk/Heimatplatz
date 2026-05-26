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

            // DROP Portal-Felder: zuerst zugehoerige Default-Constraints loeschen,
            // dann die Spalte (SQL Server erlaubt DROP COLUMN nicht solange Constraints anhaengen).
            migrationBuilder.Sql(DropColumnSql("UserFilterPreferences", "IsPortalSelected"));
            migrationBuilder.Sql(DropColumnSql("NotificationPreferences", "IsPortalSelected"));
            migrationBuilder.Sql(DropColumnSql("NotificationPreferences", "ExcludedSellerSourceIdsJson"));
            migrationBuilder.Sql(DropColumnSql("SellerSources", "SellerType"));

            // ADD SelectedSort wenn fehlt
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'SelectedSort' AND Object_ID = Object_ID(N'UserFilterPreferences'))
    ALTER TABLE [UserFilterPreferences] ADD [SelectedSort] int NOT NULL CONSTRAINT [DF_UserFilterPreferences_SelectedSort] DEFAULT 0;
");
        }
        else
        {
            // --- SQLite: faktisch No-op, da Dev-DB via EnsureCreatedAsync bereits korrektes Schema hat ---
        }
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Down ist nicht sinnvoll: wir wuerden Schema in einen historischen, kaputten Zustand zurueckversetzen.
    }

    /// <summary>
    /// Erzeugt T-SQL das eine Spalte idempotent droppt:
    /// 1. Loescht alle Default-Constraints, die auf die Spalte zeigen.
    /// 2. Loescht die Spalte falls sie existiert.
    /// SQL Server vergibt Default-Constraint-Namen automatisch (z.B. DF__UserFilte__IsPor__1F98B2C1),
    /// daher muessen wir sie zur Laufzeit nachschlagen.
    /// </summary>
    private static string DropColumnSql(string table, string column) => $@"
DECLARE @constraint_name nvarchar(200);
SELECT @constraint_name = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.default_object_id = dc.object_id
WHERE c.object_id = OBJECT_ID('{table}') AND c.name = N'{column}';
IF @constraint_name IS NOT NULL
    EXEC('ALTER TABLE [{table}] DROP CONSTRAINT [' + @constraint_name + ']');

IF EXISTS (SELECT 1 FROM sys.columns WHERE Name = N'{column}' AND Object_ID = OBJECT_ID(N'{table}'))
    ALTER TABLE [{table}] DROP COLUMN [{column}];
";
}
