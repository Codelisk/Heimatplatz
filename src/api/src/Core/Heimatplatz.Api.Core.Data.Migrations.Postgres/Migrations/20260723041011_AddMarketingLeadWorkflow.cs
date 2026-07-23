using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingLeadWorkflow : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_MarketingContacts_Email",
                table: "MarketingContacts");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "MarketingContacts",
                type: "character varying(320)",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "MarketingContacts",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmenbuchFnr",
                table: "MarketingContacts",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "NextFollowUpAt",
                table: "MarketingContacts",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketingActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ContactId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Notes = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    StatusFrom = table.Column<int>(type: "integer", nullable: true),
                    StatusTo = table.Column<int>(type: "integer", nullable: true),
                    FollowUpAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingActivities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingActivities_MarketingContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "MarketingContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingEmailTemplates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingEmailTemplates", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_Email",
                table: "MarketingContacts",
                column: "Email",
                unique: true,
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_FirmenbuchFnr",
                table: "MarketingContacts",
                column: "FirmenbuchFnr",
                unique: true,
                filter: "\"FirmenbuchFnr\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_NextFollowUpAt",
                table: "MarketingContacts",
                column: "NextFollowUpAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingActivities_ContactId_OccurredAt",
                table: "MarketingActivities",
                columns: new[] { "ContactId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmailTemplates_DisplayOrder",
                table: "MarketingEmailTemplates",
                column: "DisplayOrder");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmailTemplates_Name",
                table: "MarketingEmailTemplates",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingActivities");

            migrationBuilder.DropTable(
                name: "MarketingEmailTemplates");

            migrationBuilder.DropIndex(
                name: "IX_MarketingContacts_Email",
                table: "MarketingContacts");

            migrationBuilder.DropIndex(
                name: "IX_MarketingContacts_FirmenbuchFnr",
                table: "MarketingContacts");

            migrationBuilder.DropIndex(
                name: "IX_MarketingContacts_NextFollowUpAt",
                table: "MarketingContacts");

            migrationBuilder.DropColumn(
                name: "City",
                table: "MarketingContacts");

            migrationBuilder.DropColumn(
                name: "FirmenbuchFnr",
                table: "MarketingContacts");

            migrationBuilder.DropColumn(
                name: "NextFollowUpAt",
                table: "MarketingContacts");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "MarketingContacts",
                type: "character varying(320)",
                maxLength: 320,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(320)",
                oldMaxLength: 320,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_Email",
                table: "MarketingContacts",
                column: "Email",
                unique: true);
        }
    }
}
