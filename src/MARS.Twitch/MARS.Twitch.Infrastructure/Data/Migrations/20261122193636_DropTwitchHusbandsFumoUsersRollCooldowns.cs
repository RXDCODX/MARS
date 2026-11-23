using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Twitch.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class DropTwitchHusbandsFumoUsersRollCooldowns : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "FumoUsers", schema: "twitch");

        migrationBuilder.DropTable(name: "Husbands", schema: "twitch");

        migrationBuilder.DropTable(name: "RollCooldowns", schema: "twitch");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "FumoUsers",
            schema: "twitch",
            columns: table => new
            {
                TwitchId = table.Column<string>(type: "character varying(50)", nullable: false),
                LastTime = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_FumoUsers", x => x.TwitchId);
                table.ForeignKey(
                    name: "FK_FumoUsers_TwitchUsers_TwitchId",
                    column: x => x.TwitchId,
                    principalSchema: "twitch",
                    principalTable: "TwitchUsers",
                    principalColumn: "TwitchId",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "Husbands",
            schema: "twitch",
            columns: table => new
            {
                TwitchId = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false
                ),
                IsPrivated = table.Column<bool>(type: "boolean", nullable: false),
                LastWeddingCongratulatedMonths = table.Column<int>(type: "integer", nullable: true),
                WhenPrivated = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: true
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Husbands", x => x.TwitchId);
                table.ForeignKey(
                    name: "FK_Husbands_TwitchUsers_TwitchId",
                    column: x => x.TwitchId,
                    principalSchema: "twitch",
                    principalTable: "TwitchUsers",
                    principalColumn: "TwitchId",
                    onDelete: ReferentialAction.Restrict
                );
            }
        );

        migrationBuilder.CreateTable(
            name: "RollCooldowns",
            schema: "twitch",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                LastRollTime = table.Column<DateTime>(
                    type: "timestamp with time zone",
                    nullable: false
                ),
                RollType = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false
                ),
                TwitchUserId = table.Column<string>(
                    type: "character varying(50)",
                    maxLength: 50,
                    nullable: false
                ),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RollCooldowns", x => x.Id);
            }
        );

        migrationBuilder.CreateIndex(
            name: "IX_RollCooldowns_TwitchUserId_RollType",
            schema: "twitch",
            table: "RollCooldowns",
            columns: new[] { "TwitchUserId", "RollType" },
            unique: true
        );
    }
}
