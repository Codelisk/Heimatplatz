using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWkoCompanyFirmenbuchFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Euid",
                table: "WkoCompanies",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FirmenbuchEnrichedAt",
                table: "WkoCompanies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FirmenbuchFoundedDate",
                table: "WkoCompanies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmenbuchManagingDirectors",
                table: "WkoCompanies",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Euid",
                table: "WkoCompanies");

            migrationBuilder.DropColumn(
                name: "FirmenbuchEnrichedAt",
                table: "WkoCompanies");

            migrationBuilder.DropColumn(
                name: "FirmenbuchFoundedDate",
                table: "WkoCompanies");

            migrationBuilder.DropColumn(
                name: "FirmenbuchManagingDirectors",
                table: "WkoCompanies");
        }
    }
}
