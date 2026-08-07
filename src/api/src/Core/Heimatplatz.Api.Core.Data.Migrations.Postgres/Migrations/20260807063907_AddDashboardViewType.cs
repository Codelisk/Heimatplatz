using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddDashboardViewType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ViewType",
                table: "UserDashboards",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ViewType",
                table: "UserDashboards");
        }
    }
}
