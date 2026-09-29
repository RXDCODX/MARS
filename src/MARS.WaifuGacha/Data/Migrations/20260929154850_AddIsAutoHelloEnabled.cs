using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.WaifuGacha.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddIsAutoHelloEnabled : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsAutoHelloEnabled",
                schema: "waifu",
                table: "Husbands",
                type: "boolean",
                nullable: false,
                defaultValue: false
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsAutoHelloEnabled",
                schema: "waifu",
                table: "Husbands"
            );
        }
    }
}
