using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Provider-aware + idempotent: auf einer Legacy-DB (EnsureCreatedAsync mit bereits
            // aktuellem Entity-Modell) kann ListingAnalyses schon existieren - ein ungeschuetztes
            // CreateTable wuerde MigrateAsync mit "table already exists" crashen lassen. Ausserdem
            // sind die hart auf SQLite ("TEXT"/"INTEGER") gescaffoldeten Typen auf SQL Server
            // (Produktion) ungueltig fuer Key-/Index-Spalten (vgl. FixProductionSchemaDrift).
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

            if (isSqlServer)
            {
                migrationBuilder.Sql(@"
IF OBJECT_ID(N'[ListingAnalyses]', 'U') IS NULL
BEGIN
    CREATE TABLE [ListingAnalyses] (
        [Id] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Status] int NOT NULL,
        [ImageUrls] nvarchar(max) NOT NULL,
        [VideoUrls] nvarchar(max) NOT NULL,
        [DictatedText] nvarchar(8000) NULL,
        [UserNotes] nvarchar(4000) NULL,
        [ResultJson] nvarchar(max) NULL,
        [ErrorMessage] nvarchar(4000) NULL,
        [StartedAt] datetimeoffset NULL,
        [CompletedAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [UpdatedAt] datetimeoffset NULL,
        CONSTRAINT [PK_ListingAnalyses] PRIMARY KEY ([Id])
    );
END
");
                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ListingAnalyses_Status' AND object_id = OBJECT_ID('ListingAnalyses'))
    CREATE INDEX [IX_ListingAnalyses_Status] ON [ListingAnalyses]([Status]);
");
                migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_ListingAnalyses_UserId' AND object_id = OBJECT_ID('ListingAnalyses'))
    CREATE INDEX [IX_ListingAnalyses_UserId] ON [ListingAnalyses]([UserId]);
");
            }
            else
            {
                migrationBuilder.Sql(@"
CREATE TABLE IF NOT EXISTS ""ListingAnalyses"" (
    ""Id"" TEXT NOT NULL CONSTRAINT ""PK_ListingAnalyses"" PRIMARY KEY,
    ""UserId"" TEXT NOT NULL,
    ""Status"" INTEGER NOT NULL,
    ""ImageUrls"" TEXT NOT NULL,
    ""VideoUrls"" TEXT NOT NULL,
    ""DictatedText"" TEXT NULL,
    ""UserNotes"" TEXT NULL,
    ""ResultJson"" TEXT NULL,
    ""ErrorMessage"" TEXT NULL,
    ""StartedAt"" TEXT NULL,
    ""CompletedAt"" TEXT NULL,
    ""CreatedAt"" TEXT NOT NULL,
    ""UpdatedAt"" TEXT NULL
);
");
                migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_ListingAnalyses_Status\" ON \"ListingAnalyses\" (\"Status\");");
                migrationBuilder.Sql("CREATE INDEX IF NOT EXISTS \"IX_ListingAnalyses_UserId\" ON \"ListingAnalyses\" (\"UserId\");");
            }
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP TABLE IF EXISTS \"ListingAnalyses\";");
        }
    }
}
