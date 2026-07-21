using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    CategoryText = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Street = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    PostalCode = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    City = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Phones = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Website = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    OpeningHoursText = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                    CompanyRegisterNumber = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    CompanyCourt = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Gln = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LegalForm = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    FoundedYear = table.Column<int>(type: "INTEGER", nullable: true),
                    IsTrainingCompany = table.Column<bool>(type: "INTEGER", nullable: false),
                    Permits = table.Column<string>(type: "TEXT", nullable: false),
                    WkoFirmaId = table.Column<Guid>(type: "TEXT", nullable: false),
                    DetailUrl = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                    SourceSearchTerm = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    ContentHash = table.Column<string>(type: "TEXT", maxLength: 64, nullable: true),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    FirstSeenAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastScrapedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    RemovedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
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
