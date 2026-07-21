using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddWkoCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WkoCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    CategoryText = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Street = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Phones = table.Column<string>(type: "text", nullable: false),
                    Email = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Website = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    OpeningHoursText = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CompanyRegisterNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    CompanyCourt = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    Gln = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    LegalForm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    FoundedYear = table.Column<int>(type: "integer", nullable: true),
                    IsTrainingCompany = table.Column<bool>(type: "boolean", nullable: false),
                    Permits = table.Column<string>(type: "text", nullable: false),
                    WkoFirmaId = table.Column<Guid>(type: "uuid", nullable: false),
                    DetailUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    SourceSearchTerm = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastScrapedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    RemovedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WkoCompanies", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WkoCompanies_City",
                table: "WkoCompanies",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_WkoCompanies_CreatedAt",
                table: "WkoCompanies",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_WkoCompanies_IsActive",
                table: "WkoCompanies",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_WkoCompanies_PostalCode",
                table: "WkoCompanies",
                column: "PostalCode");

            migrationBuilder.CreateIndex(
                name: "IX_WkoCompanies_WkoFirmaId",
                table: "WkoCompanies",
                column: "WkoFirmaId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WkoCompanies");
        }
    }
}
