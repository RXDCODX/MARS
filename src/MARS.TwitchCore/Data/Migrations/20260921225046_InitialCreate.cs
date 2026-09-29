using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.TwitchCore.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "twitch");

            migrationBuilder.CreateTable(
                name: "AutoMessages",
                schema: "twitch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Message = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoMessages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ChannelRewards",
                schema: "twitch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "text", nullable: false),
                    Cost = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    Prompt = table.Column<string>(type: "text", nullable: true),
                    BackgroundColor = table.Column<string>(type: "text", nullable: true),
                    IsUserInputRequired = table.Column<bool>(type: "boolean", nullable: false),
                    IsMaxPerStreamEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPerStream = table.Column<int>(type: "integer", nullable: true),
                    IsMaxPerUserPerStreamEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    MaxPerUserPerStream = table.Column<int>(type: "integer", nullable: true),
                    IsGlobalCooldownEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    GlobalCooldownSeconds = table.Column<int>(type: "integer", nullable: true),
                    ShouldRedemptionsSkipRequestQueue = table.Column<bool>(type: "boolean", nullable: false),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    TwitchRewardId = table.Column<string>(type: "text", nullable: true),
                    MediaInfoId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ChannelRewards", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RollCooldowns",
                schema: "twitch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TwitchUserId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    RollType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LastRollTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollCooldowns", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RootState",
                schema: "twitch",
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
                name: "SevenTvEmotes",
                schema: "twitch",
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
                name: "TwitchToken",
                schema: "twitch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccessToken = table.Column<string>(type: "text", nullable: false),
                    RefreshToken = table.Column<string>(type: "text", nullable: false),
                    ExpiresIn = table.Column<TimeSpan>(type: "interval", nullable: false),
                    WhenCreated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchToken", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TwitchUsers",
                schema: "twitch",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    UserLogin = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProfileImageUrl = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    ChatColor = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    IsModerator = table.Column<bool>(type: "boolean", nullable: false),
                    IsVip = table.Column<bool>(type: "boolean", nullable: false),
                    FollowedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUpdated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AliasNickname = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    IsInBlockList = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchUsers", x => x.TwitchId);
                });

            migrationBuilder.CreateTable(
                name: "FollowersEntitys",
                schema: "twitch",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "character varying(50)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FollowersEntitys", x => x.UserId);
                    table.ForeignKey(
                        name: "FK_FollowersEntitys_TwitchUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "twitch",
                        principalTable: "TwitchUsers",
                        principalColumn: "TwitchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FumoUsers",
                schema: "twitch",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "character varying(50)", nullable: false),
                    LastTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "HelloVideosUsers",
                schema: "twitch",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TwitchId = table.Column<string>(type: "character varying(50)", nullable: false),
                    LastTimeNotif = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    MediaInfoId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HelloVideosUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HelloVideosUsers_TwitchUsers_TwitchId",
                        column: x => x.TwitchId,
                        principalSchema: "twitch",
                        principalTable: "TwitchUsers",
                        principalColumn: "TwitchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Husbands",
                schema: "twitch",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    IsPrivated = table.Column<bool>(type: "boolean", nullable: false),
                    WhenPrivated = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastWeddingCongratulatedMonths = table.Column<int>(type: "integer", nullable: true)
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
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TwitchLeaderboardUsers",
                schema: "twitch",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "character varying(50)", nullable: false),
                    RussianRouletteWins = table.Column<int>(type: "integer", nullable: false),
                    RussianRouletteWinsWithWaifu = table.Column<int>(type: "integer", nullable: false),
                    TriviaWins = table.Column<int>(type: "integer", nullable: false),
                    TriviaWinsWithWaifus = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TwitchLeaderboardUsers", x => x.TwitchId);
                    table.ForeignKey(
                        name: "FK_TwitchLeaderboardUsers_TwitchUsers_TwitchId",
                        column: x => x.TwitchId,
                        principalSchema: "twitch",
                        principalTable: "TwitchUsers",
                        principalColumn: "TwitchId",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_HelloVideosUsers_TwitchId",
                schema: "twitch",
                table: "HelloVideosUsers",
                column: "TwitchId");

            migrationBuilder.CreateIndex(
                name: "IX_RollCooldowns_TwitchUserId_RollType",
                schema: "twitch",
                table: "RollCooldowns",
                columns: new[] { "TwitchUserId", "RollType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AutoMessages",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "ChannelRewards",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "FollowersEntitys",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "FumoUsers",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "HelloVideosUsers",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "Husbands",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "RollCooldowns",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "RootState",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "SevenTvEmotes",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "TwitchLeaderboardUsers",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "TwitchToken",
                schema: "twitch");

            migrationBuilder.DropTable(
                name: "TwitchUsers",
                schema: "twitch");
        }
    }
}
