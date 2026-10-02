using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Videos365.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialVideos365 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "videos365");

            migrationBuilder.CreateTable(
                name: "Videos365",
                schema: "videos365",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SiteId = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    PlayerUrl = table.Column<string>(type: "text", nullable: false),
                    DirectLinkUrl = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    DownloadUrl = table.Column<string>(type: "text", nullable: false),
                    DateUpload = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: false),
                    IsUploaded = table.Column<bool>(type: "boolean", nullable: false),
                    VideoHeight = table.Column<int>(type: "integer", nullable: false),
                    VideoWidth = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Videos365", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_Videos365_SiteId",
                schema: "videos365",
                table: "Videos365",
                column: "SiteId",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Videos365", schema: "videos365");
        }
    }
}
