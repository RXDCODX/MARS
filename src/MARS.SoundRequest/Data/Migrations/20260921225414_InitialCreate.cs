using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.SoundRequest.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "media");

            migrationBuilder.CreateTable(
                name: "RootState",
                schema: "media",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TypeDescription = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootState", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "Tracks",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackName = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: false),
                    Authors = table.Column<string[]>(type: "text[]", nullable: true),
                    Duration = table.Column<TimeSpan>(type: "interval", nullable: false),
                    Url = table.Column<string>(type: "text", nullable: false),
                    LastTimePlays = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArtworkUrl = table.Column<string>(type: "text", nullable: true),
                    VideoId = table.Column<string>(type: "text", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tracks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "QueueItems",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackId = table.Column<Guid>(type: "uuid", nullable: false),
                    QueueOrder = table.Column<int>(type: "integer", nullable: false),
                    RequestedByTwitchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_QueueItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_QueueItems_Tracks_TrackId",
                        column: x => x.TrackId,
                        principalSchema: "media",
                        principalTable: "Tracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PlayerStates",
                schema: "media",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CurrentQueueItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentTrackProgress = table.Column<TimeSpan>(type: "interval", nullable: true),
                    State = table.Column<int>(type: "integer", nullable: false),
                    VideoState = table.Column<int>(type: "integer", nullable: false),
                    IsMuted = table.Column<bool>(type: "boolean", nullable: false),
                    PausedByMute = table.Column<bool>(type: "boolean", nullable: false),
                    Volume = table.Column<float>(type: "real", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlayerStates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlayerStates_QueueItems_CurrentQueueItemId",
                        column: x => x.CurrentQueueItemId,
                        principalSchema: "media",
                        principalTable: "QueueItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PlayerStates_CurrentQueueItemId",
                schema: "media",
                table: "PlayerStates",
                column: "CurrentQueueItemId");

            migrationBuilder.CreateIndex(
                name: "IX_QueueItems_TrackId",
                schema: "media",
                table: "QueueItems",
                column: "TrackId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PlayerStates",
                schema: "media");

            migrationBuilder.DropTable(
                name: "RootState",
                schema: "media");

            migrationBuilder.DropTable(
                name: "QueueItems",
                schema: "media");

            migrationBuilder.DropTable(
                name: "Tracks",
                schema: "media");
        }
    }
}
