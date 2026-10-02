using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.MediaStorage.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaStorageEntries : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MediaEntries",
                schema: "mediastorage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Path = table.Column<string>(type: "text", nullable: false),
                    FileName = table.Column<string>(type: "text", nullable: false),
                    Extension = table.Column<string>(type: "text", nullable: false),
                    MediaType = table.Column<int>(type: "integer", nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastDownloadedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    DeletedAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    OriginalPath = table.Column<string>(type: "text", nullable: true),
                    ContentHash = table.Column<string>(type: "text", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MediaEntries", x => x.Id);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_DeletedAt",
                schema: "mediastorage",
                table: "MediaEntries",
                column: "DeletedAt"
            );

            migrationBuilder.CreateIndex(
                name: "IX_MediaEntries_Path",
                schema: "mediastorage",
                table: "MediaEntries",
                column: "Path",
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "MediaEntries", schema: "mediastorage");
        }
    }
}
