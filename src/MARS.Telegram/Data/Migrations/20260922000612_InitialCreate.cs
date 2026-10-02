using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MARS.Telegram.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "chat");

            migrationBuilder.CreateTable(
                name: "ChannelProcessingStates",
                schema: "chat",
                columns: table => new
                {
                    ChannelId = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    OffsetId = table.Column<int>(type: "integer", nullable: false),
                    MessagesHash = table.Column<long>(type: "bigint", nullable: true),
                    LastUpdated = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelProcessingStates", x => x.ChannelId);
                }
            );

            migrationBuilder.CreateTable(
                name: "RootState",
                schema: "chat",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TypeDescription = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootState", x => x.Name);
                }
            );

            migrationBuilder.CreateTable(
                name: "TelegramDiscordChannelBindings",
                schema: "chat",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TelegramChannelId = table.Column<long>(type: "bigint", nullable: false),
                    DiscordChannelId = table.Column<string>(
                        type: "character varying(64)",
                        nullable: false
                    ),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LastError = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    CreatedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAtUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramDiscordChannelBindings", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "TelegramDiscordChannelStates",
                schema: "chat",
                columns: table => new
                {
                    TelegramChannelId = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    LastProcessedMessageId = table.Column<int>(type: "integer", nullable: false),
                    LastUpdatedUtc = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramDiscordChannelStates", x => x.TelegramChannelId);
                }
            );

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

            migrationBuilder.CreateTable(
                name: "TelegramUsers",
                schema: "chat",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    LastTimeMessage = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    RaidHelper = table.Column<bool>(type: "boolean", nullable: false),
                    PyroAlertsAccess = table.Column<bool>(type: "boolean", nullable: false),
                    IsRandomMemeSendler = table.Column<bool>(type: "boolean", nullable: false),
                    HonkaiNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    StreamUpNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    ZenlessZoneZeroDailyNotif = table.Column<bool>(
                        type: "boolean",
                        nullable: false
                    ),
                    GenshinImpactDailyNotif = table.Column<bool>(type: "boolean", nullable: false),
                    ByeByeLastMessageTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    ByeByeServiceNotification = table.Column<bool>(
                        type: "boolean",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramUsers", x => x.UserId);
                }
            );

            migrationBuilder.CreateTable(
                name: "WTelegramSessions",
                schema: "chat",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WTelegramSessions", x => x.Name);
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_TelegramDiscordChannelBindings_TelegramChannelId_DiscordCha~",
                schema: "chat",
                table: "TelegramDiscordChannelBindings",
                columns: new[] { "TelegramChannelId", "DiscordChannelId" },
                unique: true
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ChannelProcessingStates", schema: "chat");

            migrationBuilder.DropTable(name: "RootState", schema: "chat");

            migrationBuilder.DropTable(name: "TelegramDiscordChannelBindings", schema: "chat");

            migrationBuilder.DropTable(name: "TelegramDiscordChannelStates", schema: "chat");

            migrationBuilder.DropTable(name: "TelegramUpdateReceiverOffsets", schema: "chat");

            migrationBuilder.DropTable(name: "TelegramUsers", schema: "chat");

            migrationBuilder.DropTable(name: "WTelegramSessions", schema: "chat");
        }
    }
}
