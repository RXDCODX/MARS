using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MARS.MediaStorage.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "mediastorage");

            migrationBuilder.CreateTable(
                name: "Alerts",
                schema: "mediastorage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TextInfo_KeyWordsColor = table.Column<string>(type: "text", nullable: true),
                    TextInfo_TriggerWord = table.Column<string>(type: "text", nullable: true),
                    TextInfo_Text = table.Column<string>(type: "text", nullable: true),
                    TextInfo_TextColor = table.Column<string>(type: "text", nullable: true),
                    TextInfo_KeyWordSybmolDelimiter = table.Column<char>(type: "character(1)", nullable: true),
                    FileInfo_Type = table.Column<string>(type: "text", nullable: false),
                    FileInfo_LocalFilePath = table.Column<string>(type: "text", nullable: false),
                    FileInfo_IsLocal = table.Column<bool>(type: "boolean", nullable: false),
                    FileInfo_FileName = table.Column<string>(type: "text", nullable: false),
                    FileInfo_Extension = table.Column<string>(type: "text", nullable: false),
                    FileInfo_IsFileNotConvertable = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_IsProportion = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_IsResizeRequires = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_Height = table.Column<int>(type: "integer", nullable: false),
                    PositionInfo_Width = table.Column<int>(type: "integer", nullable: false),
                    PositionInfo_IsRotated = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_Rotation = table.Column<int>(type: "integer", nullable: false),
                    PositionInfo_XCoordinate = table.Column<int>(type: "integer", nullable: false),
                    PositionInfo_YCoordinate = table.Column<int>(type: "integer", nullable: false),
                    PositionInfo_RandomCoordinates = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_IsVerticallCenter = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_IsHorizontalCenter = table.Column<bool>(type: "boolean", nullable: false),
                    PositionInfo_IsUseOriginalWidthAndHeight = table.Column<bool>(type: "boolean", nullable: false),
                    MetaInfo_TwitchPointsCost = table.Column<int>(type: "integer", nullable: false),
                    MetaInfo_TwitchGuid = table.Column<Guid>(type: "uuid", nullable: true),
                    MetaInfo_VIP = table.Column<bool>(type: "boolean", nullable: false),
                    MetaInfo_DisplayName = table.Column<string>(type: "text", nullable: false),
                    MetaInfo_IsLooped = table.Column<bool>(type: "boolean", nullable: false),
                    MetaInfo_IsFreezeRequired = table.Column<bool>(type: "boolean", nullable: false),
                    MetaInfo_Duration = table.Column<int>(type: "integer", nullable: false),
                    MetaInfo_Priority = table.Column<int>(type: "integer", nullable: false),
                    MetaInfo_Volume = table.Column<int>(type: "integer", nullable: false),
                    MetaInfo_IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    StylesInfo_IsBorder = table.Column<bool>(type: "boolean", nullable: false),
                    StylesInfo_IsShowLetterbox = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RandomMemeType",
                schema: "mediastorage",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FolderPath = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandomMemeType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RandomMemeOrder",
                schema: "mediastorage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Order = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FilePath = table.Column<string>(type: "text", maxLength: 2147483647, nullable: false),
                    MemeTypeId = table.Column<int>(type: "integer", nullable: true),
                    IsFileNotConvertable = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RandomMemeOrder", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RandomMemeOrder_RandomMemeType_MemeTypeId",
                        column: x => x.MemeTypeId,
                        principalSchema: "mediastorage",
                        principalTable: "RandomMemeType",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "mediastorage",
                table: "RandomMemeType",
                columns: new[] { "Id", "FolderPath", "Name" },
                values: new object[,]
                {
                    { 2, "Alerts\\random_meme", "Random Meme" },
                    { 3, "Alerts\\zvik", "Random Sound" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_RandomMemeOrder_MemeTypeId",
                schema: "mediastorage",
                table: "RandomMemeOrder",
                column: "MemeTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts",
                schema: "mediastorage");

            migrationBuilder.DropTable(
                name: "RandomMemeOrder",
                schema: "mediastorage");

            migrationBuilder.DropTable(
                name: "RandomMemeType",
                schema: "mediastorage");
        }
    }
}
