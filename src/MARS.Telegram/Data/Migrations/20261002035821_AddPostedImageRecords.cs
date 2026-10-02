using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Telegram.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPostedImageRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PostedImageRecords",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Source = table.Column<string>(type: "text", nullable: false),
                    ImageId = table.Column<int>(type: "integer", nullable: false),
                    DiscordChannelId = table.Column<decimal>(
                        type: "numeric(20,0)",
                        nullable: false
                    ),
                    PostedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PostedImageRecords", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_PostedImageRecords_Source_ImageId_DiscordChannelId",
                schema: "chat",
                table: "PostedImageRecords",
                columns: new[] { "Source", "ImageId", "DiscordChannelId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PostedImageRecords", schema: "chat");
        }
    }
}
