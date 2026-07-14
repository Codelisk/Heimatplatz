using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
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
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppVersion",
                table: "PushSubscriptions",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DataJson",
                table: "PushSubscriptions",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DeviceId",
                table: "PushSubscriptions",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Environment",
                table: "PushSubscriptions",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Production");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ExpiresAt",
                table: "PushSubscriptions",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Locale",
                table: "PushSubscriptions",
                type: "character varying(35)",
                maxLength: 35,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TagsJson",
                table: "PushSubscriptions",
                type: "text",
                nullable: false,
                defaultValue: "[]");

            migrationBuilder.AddColumn<string>(
                name: "TopicsJson",
                table: "PushSubscriptions",
                type: "text",
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
