using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveDoorNumberFromLegalAddress : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "LegalSettings"
                SET "ResponsiblePartyJson" = REPLACE(
                    REPLACE(
                        REPLACE(
                            "ResponsiblePartyJson",
                            '"street":"Stockham 44/Tür 2"',
                            '"street":"Stockham 44"'),
                        '"street":"Stockham 44/Tuer 2"',
                        '"street":"Stockham 44"'),
                    '"street":"Stockham 44/T\u00FCr 2"',
                    '"street":"Stockham 44"')
                WHERE "ResponsiblePartyJson" LIKE '%Stockham 44/%';
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE "LegalSettings"
                SET "ResponsiblePartyJson" = REPLACE(
                    "ResponsiblePartyJson",
                    '"street":"Stockham 44"',
                    '"street":"Stockham 44/Tür 2"')
                WHERE "ResponsiblePartyJson" LIKE '%"street":"Stockham 44"%';
                """);
        }
    }
}
