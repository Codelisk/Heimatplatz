using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDocumentUrlsToForeclosureAuction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ForeclosureAuctions_State",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "State",
                table: "ForeclosureAuctions");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "BiddingDeadline",
                table: "ForeclosureAuctions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "BuildingArea",
                table: "ForeclosureAuctions",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BuildingCondition",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CadastralMunicipality",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FloorPlanUrl",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "GardenArea",
                table: "ForeclosureAuctions",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LongAppraisalUrl",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "NumberOfRooms",
                table: "ForeclosureAuctions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnershipShare",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PlotArea",
                table: "ForeclosureAuctions",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlotNumber",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RegistrationNumber",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SheetNumber",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ShortAppraisalUrl",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SitePlanUrl",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalArea",
                table: "ForeclosureAuctions",
                type: "TEXT",
                precision: 12,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ViewingDate",
                table: "ForeclosureAuctions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearBuilt",
                table: "ForeclosureAuctions",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ZoningDesignation",
                table: "ForeclosureAuctions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_RegistrationNumber",
                table: "ForeclosureAuctions",
                column: "RegistrationNumber");

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_Status",
                table: "ForeclosureAuctions",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ForeclosureAuctions_RegistrationNumber",
                table: "ForeclosureAuctions");

            migrationBuilder.DropIndex(
                name: "IX_ForeclosureAuctions_Status",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "BiddingDeadline",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "BuildingArea",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "BuildingCondition",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "CadastralMunicipality",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "FloorPlanUrl",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "GardenArea",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "LongAppraisalUrl",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "NumberOfRooms",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "OwnershipShare",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "PlotArea",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "PlotNumber",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "RegistrationNumber",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "SheetNumber",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "ShortAppraisalUrl",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "SitePlanUrl",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "TotalArea",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "ViewingDate",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "YearBuilt",
                table: "ForeclosureAuctions");

            migrationBuilder.DropColumn(
                name: "ZoningDesignation",
                table: "ForeclosureAuctions");

            migrationBuilder.AddColumn<int>(
                name: "State",
                table: "ForeclosureAuctions",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_ForeclosureAuctions_State",
                table: "ForeclosureAuctions",
                column: "State");
        }
    }
}
