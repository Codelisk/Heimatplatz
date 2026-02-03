using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMunicipalityFkToProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Properties_City",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_SourceName_SourceId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "City",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Properties");

            migrationBuilder.AddColumn<Guid>(
                name: "MunicipalityId",
                table: "Properties",
                type: "TEXT",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Properties_MunicipalityId",
                table: "Properties",
                column: "MunicipalityId");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_SourceName_SourceId",
                table: "Properties",
                columns: new[] { "SourceName", "SourceId" },
                unique: true,
                filter: "\"SourceName\" IS NOT NULL AND \"SourceId\" IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Properties_Municipalities_MunicipalityId",
                table: "Properties",
                column: "MunicipalityId",
                principalTable: "Municipalities",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Properties_Municipalities_MunicipalityId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_MunicipalityId",
                table: "Properties");

            migrationBuilder.DropIndex(
                name: "IX_Properties_SourceName_SourceId",
                table: "Properties");

            migrationBuilder.DropColumn(
                name: "MunicipalityId",
                table: "Properties");

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Properties",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Properties",
                type: "TEXT",
                maxLength: 10,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_City",
                table: "Properties",
                column: "City");

            migrationBuilder.CreateIndex(
                name: "IX_Properties_SourceName_SourceId",
                table: "Properties",
                columns: new[] { "SourceName", "SourceId" },
                unique: true,
                filter: "[SourceName] IS NOT NULL AND [SourceId] IS NOT NULL");
        }
    }
}
