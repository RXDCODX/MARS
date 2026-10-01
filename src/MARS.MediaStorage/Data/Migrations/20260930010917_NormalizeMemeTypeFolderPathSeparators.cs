using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.MediaStorage.Data.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeMemeTypeFolderPathSeparators : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "mediastorage",
                table: "RandomMemeType",
                keyColumn: "Id",
                keyValue: 2,
                column: "FolderPath",
                value: "Alerts/random_meme");

            migrationBuilder.UpdateData(
                schema: "mediastorage",
                table: "RandomMemeType",
                keyColumn: "Id",
                keyValue: 3,
                column: "FolderPath",
                value: "Alerts/zvik");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                schema: "mediastorage",
                table: "RandomMemeType",
                keyColumn: "Id",
                keyValue: 2,
                column: "FolderPath",
                value: "Alerts\\random_meme");

            migrationBuilder.UpdateData(
                schema: "mediastorage",
                table: "RandomMemeType",
                keyColumn: "Id",
                keyValue: 3,
                column: "FolderPath",
                value: "Alerts\\zvik");
        }
    }
}
