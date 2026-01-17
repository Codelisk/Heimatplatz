using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddTypeSpecificDataToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TypeSpecificData",
                table: "Properties",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TypeSpecificData",
                table: "Properties");
        }
    }
}
