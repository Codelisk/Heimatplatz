using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddListingAnalyses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ListingAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    UserId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    ImageUrls = table.Column<string>(type: "TEXT", nullable: false),
                    VideoUrls = table.Column<string>(type: "TEXT", nullable: false),
                    DictatedText = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    UserNotes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    ResultJson = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorMessage = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ListingAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ListingAnalyses_Status",
                table: "ListingAnalyses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_ListingAnalyses_UserId",
                table: "ListingAnalyses",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ListingAnalyses");
        }
    }
}
