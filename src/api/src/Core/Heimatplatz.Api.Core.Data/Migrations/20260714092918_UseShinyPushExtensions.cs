using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class UseShinyPushExtensions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AppId",
                table: "PushSubscriptions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppVersion",
                table: "PushSubscriptions",
                type: "TEXT",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                table: "PushSubscriptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "PushSubscriptions",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "PushSubscriptions",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "Production");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "PushSubscriptions",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "PushSubscriptions",
                type: "TEXT",
                maxLength: 35,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "PushSubscriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "TopicsJson",
                table: "PushSubscriptions",
                type: "TEXT",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.CreateIndex(
                name: "IX_PushSubscriptions_DeviceId",
                table: "PushSubscriptions",
                column: "DeviceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PushSubscriptions_DeviceId",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "AppId",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "AppVersion",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "DataJson",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "DeviceId",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "Environment",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "ExpiresAt",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "Locale",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "TagsJson",
                table: "PushSubscriptions");

            migrationBuilder.DropColumn(
                name: "TopicsJson",
                table: "PushSubscriptions");
        }
    }
}
