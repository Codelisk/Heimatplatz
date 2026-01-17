using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Heimatplatz.Api.Core.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenamePropertyFieldsToEnglish : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Zimmer",
                table: "Properties",
                newName: "YearBuilt");

            migrationBuilder.RenameColumn(
                name: "WohnflaecheM2",
                table: "Properties",
                newName: "Rooms");

            migrationBuilder.RenameColumn(
                name: "Typ",
                table: "Properties",
                newName: "Type");

            migrationBuilder.RenameColumn(
                name: "Titel",
                table: "Properties",
                newName: "Title");

            migrationBuilder.RenameColumn(
                name: "Preis",
                table: "Properties",
                newName: "Price");

            migrationBuilder.RenameColumn(
                name: "Plz",
                table: "Properties",
                newName: "PostalCode");

            migrationBuilder.RenameColumn(
                name: "Ort",
                table: "Properties",
                newName: "City");

            migrationBuilder.RenameColumn(
                name: "GrundstuecksflaecheM2",
                table: "Properties",
                newName: "PlotAreaSquareMeters");

            migrationBuilder.RenameColumn(
                name: "BildUrls",
                table: "Properties",
                newName: "ImageUrls");

            migrationBuilder.RenameColumn(
                name: "Beschreibung",
                table: "Properties",
                newName: "Description");

            migrationBuilder.RenameColumn(
                name: "Baujahr",
                table: "Properties",
                newName: "LivingAreaSquareMeters");

            migrationBuilder.RenameColumn(
                name: "Ausstattung",
                table: "Properties",
                newName: "Features");

            migrationBuilder.RenameColumn(
                name: "AnbieterTyp",
                table: "Properties",
                newName: "SellerType");

            migrationBuilder.RenameColumn(
                name: "AnbieterName",
                table: "Properties",
                newName: "SellerName");

            migrationBuilder.RenameColumn(
                name: "Adresse",
                table: "Properties",
                newName: "Address");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_Typ",
                table: "Properties",
                newName: "IX_Properties_Type");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_Preis",
                table: "Properties",
                newName: "IX_Properties_Price");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_Ort",
                table: "Properties",
                newName: "IX_Properties_City");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "YearBuilt",
                table: "Properties",
                newName: "Zimmer");

            migrationBuilder.RenameColumn(
                name: "Type",
                table: "Properties",
                newName: "Typ");

            migrationBuilder.RenameColumn(
                name: "Title",
                table: "Properties",
                newName: "Titel");

            migrationBuilder.RenameColumn(
                name: "SellerType",
                table: "Properties",
                newName: "AnbieterTyp");

            migrationBuilder.RenameColumn(
                name: "SellerName",
                table: "Properties",
                newName: "AnbieterName");

            migrationBuilder.RenameColumn(
                name: "Rooms",
                table: "Properties",
                newName: "WohnflaecheM2");

            migrationBuilder.RenameColumn(
                name: "Price",
                table: "Properties",
                newName: "Preis");

            migrationBuilder.RenameColumn(
                name: "PostalCode",
                table: "Properties",
                newName: "Plz");

            migrationBuilder.RenameColumn(
                name: "PlotAreaSquareMeters",
                table: "Properties",
                newName: "GrundstuecksflaecheM2");

            migrationBuilder.RenameColumn(
                name: "LivingAreaSquareMeters",
                table: "Properties",
                newName: "Baujahr");

            migrationBuilder.RenameColumn(
                name: "ImageUrls",
                table: "Properties",
                newName: "BildUrls");

            migrationBuilder.RenameColumn(
                name: "Features",
                table: "Properties",
                newName: "Ausstattung");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "Properties",
                newName: "Beschreibung");

            migrationBuilder.RenameColumn(
                name: "City",
                table: "Properties",
                newName: "Ort");

            migrationBuilder.RenameColumn(
                name: "Address",
                table: "Properties",
                newName: "Adresse");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_Type",
                table: "Properties",
                newName: "IX_Properties_Typ");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_Price",
                table: "Properties",
                newName: "IX_Properties_Preis");

            migrationBuilder.RenameIndex(
                name: "IX_Properties_City",
                table: "Properties",
                newName: "IX_Properties_Ort");
        }
    }
}
