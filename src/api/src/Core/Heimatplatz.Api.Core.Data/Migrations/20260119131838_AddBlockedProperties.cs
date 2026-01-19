using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBlockedProperties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BlockedProperties",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    PropertyId = table.Column<Guid>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BlockedProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BlockedProperties_Properties_PropertyId",
                        column: x => x.PropertyId,
                        principalTable: "Properties",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BlockedProperties_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_CreatedAt",
                table: "BlockedProperties",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_PropertyId",
                table: "BlockedProperties",
                column: "PropertyId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_UserId",
                table: "BlockedProperties",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_BlockedProperties_UserId_PropertyId",
                table: "BlockedProperties",
                columns: new[] { "UserId", "PropertyId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BlockedProperties");
        }
    }
}
