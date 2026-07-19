using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddTelemetryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsHidden",
                table: "Properties",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "TelemetryErrorGroups",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FingerprintHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Title = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    SampleMessage = table.Column<string>(type: "TEXT", nullable: false),
                    SampleStackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    FirstSeenUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OccurrenceCount = table.Column<long>(type: "bigint", nullable: false),
                    LastTraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    Status = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryErrorGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetryLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TimestampUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    SpanId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Level = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    Category = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    EventId = table.Column<int>(type: "integer", nullable: false),
                    MessageTemplate = table.Column<string>(type: "TEXT", nullable: true),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    ExceptionType = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    ExceptionMessage = table.Column<string>(type: "TEXT", nullable: true),
                    ExceptionStackTrace = table.Column<string>(type: "TEXT", nullable: true),
                    ErrorGroupId = table.Column<Guid>(type: "uuid", nullable: true),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClientApp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Source = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    AttributesJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetryLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TelemetrySpans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TraceId = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    SpanId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ParentSpanId = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    Name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Kind = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StartTimeUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<double>(type: "double precision", nullable: false),
                    StatusCode = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    StatusDescription = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    HttpMethod = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: true),
                    HttpRoute = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    UserId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    ClientApp = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    AttributesJson = table.Column<string>(type: "TEXT", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelemetrySpans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryErrorGroups_FingerprintHash",
                table: "TelemetryErrorGroups",
                column: "FingerprintHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryErrorGroups_Status_LastSeenUtc",
                table: "TelemetryErrorGroups",
                columns: new[] { "Status", "LastSeenUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryLogs_ErrorGroupId_TimestampUtc",
                table: "TelemetryLogs",
                columns: new[] { "ErrorGroupId", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryLogs_Level_TimestampUtc",
                table: "TelemetryLogs",
                columns: new[] { "Level", "TimestampUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryLogs_TimestampUtc",
                table: "TelemetryLogs",
                column: "TimestampUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetryLogs_TraceId",
                table: "TelemetryLogs",
                column: "TraceId");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySpans_StartTimeUtc",
                table: "TelemetrySpans",
                column: "StartTimeUtc");

            migrationBuilder.CreateIndex(
                name: "IX_TelemetrySpans_TraceId",
                table: "TelemetrySpans",
                column: "TraceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelemetryErrorGroups");

            migrationBuilder.DropTable(
                name: "TelemetryLogs");

            migrationBuilder.DropTable(
                name: "TelemetrySpans");

            migrationBuilder.DropColumn(
                name: "IsHidden",
                table: "Properties");
        }
    }
}
