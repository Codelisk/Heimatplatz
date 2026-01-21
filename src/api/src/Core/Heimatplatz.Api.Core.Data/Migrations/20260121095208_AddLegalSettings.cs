using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLegalSettings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LegalSettings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    SettingType = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    ResponsiblePartyJson = table.Column<string>(type: "TEXT", nullable: true),
                    SectionsJson = table.Column<string>(type: "TEXT", nullable: true),
                    Version = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    EffectiveDate = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LegalSettings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LegalSettings_SettingType_EffectiveDate",
                table: "LegalSettings",
                columns: new[] { "SettingType", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_LegalSettings_SettingType_IsActive",
                table: "LegalSettings",
                columns: new[] { "SettingType", "IsActive" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LegalSettings");
        }
    }
}
