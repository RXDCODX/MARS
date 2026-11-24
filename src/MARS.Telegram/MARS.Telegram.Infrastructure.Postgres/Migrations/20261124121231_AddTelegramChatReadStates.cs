using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MARS.Telegram.Infrastructure.Postgres.Migrations
{
    /// <inheritdoc />
    public partial class AddTelegramChatReadStates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramChatReadStates",
                schema: "chat",
                columns: table => new
                {
                    ChatId = table
                        .Column<long>(type: "bigint", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    PeerKind = table.Column<int>(type: "integer", nullable: false),
                    LastReadMessageId = table.Column<int>(type: "integer", nullable: false),
                    LastReadAt = table.Column<DateTimeOffset>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramChatReadStates", x => x.ChatId);
                }
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "TelegramChatReadStates", schema: "chat");
        }
    }
}
