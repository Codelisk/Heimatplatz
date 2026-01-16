using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddForeclosureAuctions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ForeclosureAuctions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    AuctionDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    Address = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    State = table.Column<int>(type: "INTEGER", nullable: false),
                    Category = table.Column<int>(type: "INTEGER", nullable: false),
                    ObjectDescription = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    EdictUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    EstimatedValue = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    MinimumBid = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: true),
                    CaseNumber = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Court = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForeclosureAuctions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_AuctionDate",
                table: "ForeclosureAuctions",
                column: "AuctionDate");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_Category",
                table: "ForeclosureAuctions",
                column: "Category");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_City",
                table: "ForeclosureAuctions",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_CreatedAt",
                table: "ForeclosureAuctions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_PostalCode",
                table: "ForeclosureAuctions",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_State",
                table: "ForeclosureAuctions",
                column: "State");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForeclosureAuctions");
        }
    }
}
