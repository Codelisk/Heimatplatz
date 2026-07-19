using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <summary>
    /// Admin-Moderation: IsHidden-Spalte auf Properties (ausgeblendete Inserate).
    /// Bewusst ohne die parallel entstandenen Telemetry-Tabellen - die bringt das
    /// Telemetry-Feature in einer eigenen Migration mit (Pendant im Postgres-Set:
    /// AddTelemetryTables, dort sind beide Aenderungen zusammen gelandet).
    /// </summary>
    public partial class AddPropertyIsHidden : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Properties",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Properties");
        }
    }
}
