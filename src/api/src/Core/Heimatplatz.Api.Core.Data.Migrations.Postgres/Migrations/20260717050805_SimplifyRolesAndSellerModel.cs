using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class SimplifyRolesAndSellerModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRoles");

            // ACHTUNG: EF hat die Renames beim Scaffolding ueber Kreuz geraten (Vorname->LastName) -
            // manuell korrigiert, muss der SQLite-Migration entsprechen: Vorname=FirstName, Nachname=LastName.
            migrationBuilder.RenameColumn(
                name: "Vorname",
                table: "Users",
                newName: "FirstName");

            migrationBuilder.RenameColumn(
                name: "Nachname",
                table: "Users",
                newName: "LastName");

            // Bestandsdaten: E-Mails normalisieren (Registrierung/Login vergleichen ab jetzt lowercase)
            migrationBuilder.Sql("UPDATE \"Users\" SET \"Email\" = lower(trim(\"Email\"))");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAdmin",
                table: "Users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "SellerSourceId",
                table: "Properties",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Properties_SellerSourceId",
                table: "Properties",
                column: "SellerSourceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_SellerSources_SellerSourceId",
                table: "Properties",
                column: "SellerSourceId",
                principalTable: "SellerSources",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_SellerSources_SellerSourceId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_SellerSourceId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "IsAdmin",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SellerSourceId",
                table: "Properties");

            migrationBuilder.RenameColumn(
                name: "FirstName",
                table: "Users",
                newName: "Vorname");

            migrationBuilder.RenameColumn(
                name: "LastName",
                table: "Users",
                newName: "Nachname");

            migrationBuilder.AlterColumn<string>(
                name: "CompanyName",
                table: "Users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.CreateTable(
                name: "UserRoles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RoleType = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_UserId_RoleType",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleType" },
                unique: true);
        }
    }
}
