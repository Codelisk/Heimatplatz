using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserFilterPreferences : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UserFilterPreferences",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    SelectedOrtesJson = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: false, defaultValue: "[]"),
                    SelectedAgeFilter = table.Column<int>(type: "INTEGER", nullable: false, defaultValue: 0),
                    IsHausSelected = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsGrundstueckSelected = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsZwangsversteigerungSelected = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFilterPreferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserFilterPreferences_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserFilterPreferences_UserId",
                table: "UserFilterPreferences",
                column: "UserId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserFilterPreferences");
        }
    }
}
