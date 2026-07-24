using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWkoCompanies : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WkoCompanies");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WkoCompanies",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    CategoryText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    CompanyCourt = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    CompanyRegisterNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    DetailUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Euid = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    FirmenbuchEnrichedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FirmenbuchFoundedDate = table.Column<long>(type: "INTEGER", nullable: true),
                    FirmenbuchManagingDirectors = table.Column<string>(type: "TEXT", nullable: false),
                    FirstSeenAt = table.Column<long>(type: "INTEGER", nullable: true),
                    FoundedDate = table.Column<long>(type: "INTEGER", nullable: true),
                    FoundedYear = table.Column<int>(type: "INTEGER", nullable: true),
                    Gln = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    IsTrainingCompany = table.Column<bool>(type: "INTEGER", nullable: false),
                    LastScrapedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LegalForm = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    OpeningHoursText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    Permits = table.Column<string>(type: "TEXT", nullable: false),
                    Phones = table.Column<string>(type: "TEXT", nullable: false),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    RemovedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    SourceSearchTerm = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Street = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    WkoFirmaId = table.Column<Guid>(type: "TEXT", nullable: false)
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
                name: "IX_WkoCompanies_FoundedDate",
                table: "WkoCompanies",
                column: "FoundedDate");

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
    }
}
