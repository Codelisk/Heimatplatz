using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNewBuildProjectFlag : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsNeubauprojektSelected",
                table: "UserFilterPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsNewBuildProject",
                table: "Properties",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "Partners",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Category = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    WebsiteUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LogoUrl = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Region = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PartnerSinceYear = table.Column<int>(type: "INTEGER", nullable: true),
                    SourceName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    SellerName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    IsVisible = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Partners", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Partners_IsVisible_DisplayOrder",
                table: "Partners",
                columns: new[] { "IsVisible", "DisplayOrder" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Partners");

            migrationBuilder.DropColumn(
                name: "IsNeubauprojektSelected",
                table: "UserFilterPreferences");

            migrationBuilder.DropColumn(
                name: "IsNewBuildProject",
                table: "Properties");
        }
    }
}
