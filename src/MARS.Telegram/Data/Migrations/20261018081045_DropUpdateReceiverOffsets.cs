using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Telegram.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropUpdateReceiverOffsets : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TelegramUpdateReceiverOffsets", schema: "chat");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramUpdateReceiverOffsets",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Offset = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUpdateReceiverOffsets", x => x.Id);
                }
            );
        }
    }
}
