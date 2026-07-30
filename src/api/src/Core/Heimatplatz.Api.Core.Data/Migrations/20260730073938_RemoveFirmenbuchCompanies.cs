using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveFirmenbuchCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirmenbuchCompanies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FirmenbuchCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    FirstSeenAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Fnr = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    GerichtCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    GerichtText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    LastSeenAt = table.Column<long>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Rechtseigenschaft = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    RechtsformCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    RechtsformText = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Sitz = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SourceOrtNr = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FirmenbuchCompanies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_FirmenbuchCompanies_Fnr",
                table: "FirmenbuchCompanies",
                column: "Fnr",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FirmenbuchCompanies_Sitz",
                table: "FirmenbuchCompanies",
                column: "Sitz");

            migrationBuilder.CreateIndex(
                name: "IX_FirmenbuchCompanies_Status",
                table: "FirmenbuchCompanies",
                column: "Status");
        }
    }
}
