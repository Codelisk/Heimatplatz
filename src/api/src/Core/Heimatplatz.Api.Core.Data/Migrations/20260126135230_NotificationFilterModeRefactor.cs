using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class NotificationFilterModeRefactor : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_Location",
                table: "NotificationPreferences");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "Location",
                table: "NotificationPreferences");

            migrationBuilder.AddColumn<string>(
                name: "ExcludedSellerSourceIdsJson",
                table: "UserFilterPreferences",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerSelected",
                table: "UserFilterPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPortalSelected",
                table: "UserFilterPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivateSelected",
                table: "UserFilterPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "ExcludedSellerSourceIdsJson",
                table: "NotificationPreferences",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<int>(
                name: "FilterMode",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsBrokerSelected",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsGrundstueckSelected",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsHausSelected",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPortalSelected",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsPrivateSelected",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsZwangsversteigerungSelected",
                table: "NotificationPreferences",
                type: "INTEGER",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "SelectedLocationsJson",
                table: "NotificationPreferences",
                type: "TEXT",
                maxLength: 4000,
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateTable(
                name: "SellerSources",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    SellerType = table.Column<int>(type: "INTEGER", nullable: false),
                    IsDefault = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SellerSources", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerSources_Name_SellerType",
                table: "SellerSources",
                columns: new[] { "Name", "SellerType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SellerSources_SellerType",
                table: "SellerSources",
                column: "SellerType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SellerSources");

            migrationBuilder.DropIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "ExcludedSellerSourceIdsJson",
                table: "UserFilterPreferences");

            migrationBuilder.DropColumn(
                name: "IsBrokerSelected",
                table: "UserFilterPreferences");

            migrationBuilder.DropColumn(
                name: "IsPortalSelected",
                table: "UserFilterPreferences");

            migrationBuilder.DropColumn(
                name: "IsPrivateSelected",
                table: "UserFilterPreferences");

            migrationBuilder.DropColumn(
                name: "ExcludedSellerSourceIdsJson",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "FilterMode",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsBrokerSelected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsGrundstueckSelected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsHausSelected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsPortalSelected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsPrivateSelected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "IsZwangsversteigerungSelected",
                table: "NotificationPreferences");

            migrationBuilder.DropColumn(
                name: "SelectedLocationsJson",
                table: "NotificationPreferences");

            migrationBuilder.AddColumn<string>(
                name: "Location",
                table: "NotificationPreferences",
                type: "TEXT",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_Location",
                table: "NotificationPreferences",
                column: "Location");

            migrationBuilder.CreateIndex(
                name: "IX_NotificationPreferences_UserId",
                table: "NotificationPreferences",
                column: "UserId");
        }
    }
}
