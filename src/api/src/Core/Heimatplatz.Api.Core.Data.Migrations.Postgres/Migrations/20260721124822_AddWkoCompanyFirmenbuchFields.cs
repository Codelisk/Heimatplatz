using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
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
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirmenbuchEnrichedAt",
                table: "WkoCompanies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FirmenbuchFoundedDate",
                table: "WkoCompanies",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmenbuchManagingDirectors",
                table: "WkoCompanies",
                type: "text",
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
