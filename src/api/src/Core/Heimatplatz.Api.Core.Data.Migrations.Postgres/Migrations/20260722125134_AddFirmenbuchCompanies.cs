using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddFirmenbuchCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FirmenbuchCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Fnr = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    Sitz = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    RechtsformCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    RechtsformText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Rechtseigenschaft = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    GerichtCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    GerichtText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    SourceOrtNr = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
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

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FirmenbuchCompanies");
        }
    }
}
