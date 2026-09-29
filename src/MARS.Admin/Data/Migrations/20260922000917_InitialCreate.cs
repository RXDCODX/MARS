using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MARS.Admin.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin");

            migrationBuilder.CreateTable(
                name: "EnvironmentVariables",
                schema: "admin",
                columns: table => new
                {
                    Key = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentVariables", x => x.Key);
                });

            migrationBuilder.CreateTable(
                name: "RootState",
                schema: "admin",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TypeDescription = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootState", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "ServiceStates",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ServiceName = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    IsServiceActive = table.Column<bool>(type: "boolean", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    LastStartTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastActivity = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceStates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SevenTvEmotes",
                schema: "admin",
                columns: table => new
                {
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LoadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SevenTvEmotes", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "StreamArchiveConfigs",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramChannelId = table.Column<decimal>(type: "numeric(20,0)", nullable: false),
                    FileNameFormat = table.Column<string>(type: "text", nullable: false),
                    CheckSpan = table.Column<TimeSpan>(type: "interval", nullable: false),
                    FolderPath = table.Column<string>(type: "text", nullable: false),
                    IsConvertFile = table.Column<bool>(type: "boolean", nullable: false),
                    FileConvertType = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamArchiveConfigs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TwitchUsers",
                schema: "admin",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserLogin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchUsers", x => x.TwitchId);
                });

            migrationBuilder.CreateTable(
                name: "StreamArchiveFiles",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ConfigId = table.Column<Guid>(type: "uuid", nullable: false),
                    OriginalFileName = table.Column<string>(type: "text", nullable: false),
                    ProcessedFileName = table.Column<string>(type: "text", nullable: false),
                    OriginalFilePath = table.Column<string>(type: "text", nullable: false),
                    OriginalFileSize = table.Column<long>(type: "bigint", nullable: false),
                    DiscoveredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ProcessingStartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ProcessingCompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ChunksCount = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamArchiveFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamArchiveFiles_StreamArchiveConfigs_ConfigId",
                        column: x => x.ConfigId,
                        principalSchema: "admin",
                        principalTable: "StreamArchiveConfigs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FollowerInfos",
                schema: "admin",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowerInfos", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_FollowerInfos_TwitchUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "admin",
                        principalTable: "TwitchUsers",
                        principalColumn: "TwitchId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StreamArchiveFileChunks",
                schema: "admin",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    ChunkNumber = table.Column<int>(type: "integer", nullable: false),
                    TotalChunks = table.Column<int>(type: "integer", nullable: false),
                    ChunkFileName = table.Column<string>(type: "text", nullable: false),
                    ChunkSize = table.Column<long>(type: "bigint", nullable: false),
                    OffsetInOriginalFile = table.Column<long>(type: "bigint", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TelegramMessageId = table.Column<long>(type: "bigint", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StreamArchiveFileChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StreamArchiveFileChunks_StreamArchiveFiles_FileId",
                        column: x => x.FileId,
                        principalSchema: "admin",
                        principalTable: "StreamArchiveFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_StreamArchiveFileChunks_FileId",
                schema: "admin",
                table: "StreamArchiveFileChunks",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_StreamArchiveFiles_ConfigId",
                schema: "admin",
                table: "StreamArchiveFiles",
                column: "ConfigId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentVariables",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "FollowerInfos",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "RootState",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "ServiceStates",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "SevenTvEmotes",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "StreamArchiveFileChunks",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "TwitchUsers",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "StreamArchiveFiles",
                schema: "admin");

            migrationBuilder.DropTable(
                name: "StreamArchiveConfigs",
                schema: "admin");
        }
    }
}
