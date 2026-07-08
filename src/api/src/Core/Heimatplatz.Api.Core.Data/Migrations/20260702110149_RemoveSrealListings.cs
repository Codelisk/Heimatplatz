using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveSrealListings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotente DROPs: auf einer Legacy-DB (EnsureCreatedAsync vor dem Sreal-Feature)
            // existieren diese Tabellen u.U. nie - ein ungeschuetztes DropTable wuerde
            // InitializeDatabaseAsync/MigrateAsync mit "no such table" crashen lassen.
            var isSqlServer = migrationBuilder.ActiveProvider == "Microsoft.EntityFrameworkCore.SqlServer";

            if (isSqlServer)
            {
                migrationBuilder.Sql("IF OBJECT_ID(N'[SrealListingChanges]', 'U') IS NOT NULL DROP TABLE [SrealListingChanges];");
                migrationBuilder.Sql("IF OBJECT_ID(N'[SrealListings]', 'U') IS NOT NULL DROP TABLE [SrealListings];");
            }
            else
            {
                migrationBuilder.Sql("DROP TABLE IF EXISTS \"SrealListingChanges\";");
                migrationBuilder.Sql("DROP TABLE IF EXISTS \"SrealListings\";");
            }

            // Vom Sreal-Sync erzeugte Properties entfernen (werden ohne Feature nie mehr aktualisiert).
            // Reine DELETEs auf Subselects sind bereits idempotent (kein Effekt bei bereits geloeschten Zeilen).
            migrationBuilder.Sql("DELETE FROM PropertyContactInfos WHERE PropertyId IN (SELECT Id FROM Properties WHERE SourceName = 'sreal.at')");
            migrationBuilder.Sql("DELETE FROM Favorites WHERE PropertyId IN (SELECT Id FROM Properties WHERE SourceName = 'sreal.at')");
            migrationBuilder.Sql("DELETE FROM BlockedProperties WHERE PropertyId IN (SELECT Id FROM Properties WHERE SourceName = 'sreal.at')");
            migrationBuilder.Sql("DELETE FROM Properties WHERE SourceName = 'sreal.at'");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SrealListings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    AgentEmail = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AgentName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AgentOffice = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    AgentPhone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    BuyingType = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Commission = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 10000, nullable: true),
                    District = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    EnergyClass = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    EnergyValue = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ExternalId = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    FGee = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    FGeeClass = table.Column<string>(type: "TEXT", maxLength: 5, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    ImageUrls = table.Column<string>(type: "TEXT", nullable: false),
                    Infrastructure = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    LastScrapedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    LivingArea = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    ObjectType = table.Column<int>(type: "INTEGER", nullable: false),
                    PlotArea = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    Price = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    PriceText = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    Rooms = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Title = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SrealListings", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SrealListingChanges",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SrealListingId = table.Column<Guid>(type: "TEXT", nullable: false),
                    ChangeType = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ChangedFields = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    NewContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    OldContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SrealListingChanges", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SrealListingChanges_SrealListings_SrealListingId",
                        column: x => x.SrealListingId,
                        principalTable: "SrealListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SrealListingChanges_ChangeType",
                table: "SrealListingChanges",
                column: "ChangeType");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListingChanges_CreatedAt",
                table: "SrealListingChanges",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListingChanges_SrealListingId",
                table: "SrealListingChanges",
                column: "SrealListingId");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_City",
                table: "SrealListings",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_CreatedAt",
                table: "SrealListings",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_ExternalId",
                table: "SrealListings",
                column: "ExternalId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_IsActive",
                table: "SrealListings",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_ObjectType",
                table: "SrealListings",
                column: "ObjectType");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_PostalCode",
                table: "SrealListings",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_Price",
                table: "SrealListings",
                column: "Price");

            migrationBuilder.CreateIndex(
                name: "IX_SrealListings_State",
                table: "SrealListings",
                column: "State");
        }
    }
}
