using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMarketingCrm : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketingContacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Phone = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    ContactType = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 8000, nullable: true),
                    Source = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    LastContactedAt = table.Column<long>(type: "INTEGER", nullable: true),
                    LastReplyAt = table.Column<long>(type: "INTEGER", nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingContacts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketingEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "TEXT", nullable: false),
                    Keywords = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: true),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    SentAt = table.Column<long>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingEmails_MarketingContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "MarketingContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "MarketingInboundEmails",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    ContactId = table.Column<Guid>(type: "TEXT", nullable: true),
                    MarketingEmailId = table.Column<Guid>(type: "TEXT", nullable: true),
                    FromAddress = table.Column<string>(type: "TEXT", maxLength: 320, nullable: false),
                    FromName = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Subject = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    BodyText = table.Column<string>(type: "TEXT", nullable: true),
                    MessageId = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    InReplyTo = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    ReceivedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    IsRead = table.Column<bool>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false),
                    UpdatedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketingInboundEmails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketingInboundEmails_MarketingContacts_ContactId",
                        column: x => x.ContactId,
                        principalTable: "MarketingContacts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_MarketingInboundEmails_MarketingEmails_MarketingEmailId",
                        column: x => x.MarketingEmailId,
                        principalTable: "MarketingEmails",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_ContactType",
                table: "MarketingContacts",
                column: "ContactType");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_Email",
                table: "MarketingContacts",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingContacts_Status",
                table: "MarketingContacts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmails_ContactId",
                table: "MarketingEmails",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmails_MessageId",
                table: "MarketingEmails",
                column: "MessageId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingEmails_SentAt",
                table: "MarketingEmails",
                column: "SentAt");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingInboundEmails_ContactId",
                table: "MarketingInboundEmails",
                column: "ContactId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingInboundEmails_IsRead",
                table: "MarketingInboundEmails",
                column: "IsRead");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingInboundEmails_MarketingEmailId",
                table: "MarketingInboundEmails",
                column: "MarketingEmailId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketingInboundEmails_MessageId",
                table: "MarketingInboundEmails",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketingInboundEmails_ReceivedAt",
                table: "MarketingInboundEmails",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketingInboundEmails");

            migrationBuilder.DropTable(
                name: "MarketingEmails");

            migrationBuilder.DropTable(
                name: "MarketingContacts");
        }
    }
}
