using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
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
                type: "TEXT",
                maxLength: 320,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 320);

            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "MarketingContacts",
                type: "TEXT",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FirmenbuchFnr",
                table: "MarketingContacts",
                type: "TEXT",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "NextFollowUpAt",
                table: "MarketingContacts",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "MarketingActivities",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 4000, nullable: true),
                    StatusFrom = table.Column<int>(type: "INTEGER", nullable: true),
                    StatusTo = table.Column<int>(type: "INTEGER", nullable: true),
                    FollowUpAt = table.Column<long>(type: "INTEGER", nullable: true),
                    OccurredAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
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
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    IsActive = table.Column<bool>(type: "INTEGER", nullable: false),
                    DisplayOrder = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
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
                type: "TEXT",
                maxLength: 320,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
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
