using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddWkoCompanyFoundedDate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "FoundedDate",
                table: "WkoCompanies",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_WkoCompanies_FoundedDate",
                table: "WkoCompanies",
                column: "FoundedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_WkoCompanies_FoundedDate",
                table: "WkoCompanies");

            migrationBuilder.DropColumn(
                name: "FoundedDate",
                table: "WkoCompanies");
        }
    }
}
