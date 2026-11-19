using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Twitch.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class RewardCodeBinding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BindingStatus",
                schema: "twitch",
                table: "ChannelRewards",
                type: "text",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmoji",
                schema: "twitch",
                table: "ChannelRewards",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "RewardCode",
                schema: "twitch",
                table: "ChannelRewards",
                type: "text",
                nullable: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChannelRewards_Cost_NormalizedEmoji",
                schema: "twitch",
                table: "ChannelRewards",
                columns: new[] { "Cost", "NormalizedEmoji" },
                unique: true,
                filter: "\"NormalizedEmoji\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChannelRewards_RewardCode",
                schema: "twitch",
                table: "ChannelRewards",
                column: "RewardCode",
                unique: true,
                filter: "\"RewardCode\" IS NOT NULL"
            );

            migrationBuilder.CreateIndex(
                name: "IX_ChannelRewards_TwitchRewardId",
                schema: "twitch",
                table: "ChannelRewards",
                column: "TwitchRewardId",
                unique: true,
                filter: "\"TwitchRewardId\" IS NOT NULL"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ChannelRewards_Cost_NormalizedEmoji",
                schema: "twitch",
                table: "ChannelRewards"
            );

            migrationBuilder.DropIndex(
                name: "IX_ChannelRewards_RewardCode",
                schema: "twitch",
                table: "ChannelRewards"
            );

            migrationBuilder.DropIndex(
                name: "IX_ChannelRewards_TwitchRewardId",
                schema: "twitch",
                table: "ChannelRewards"
            );

            migrationBuilder.DropColumn(
                name: "BindingStatus",
                schema: "twitch",
                table: "ChannelRewards"
            );

            migrationBuilder.DropColumn(
                name: "NormalizedEmoji",
                schema: "twitch",
                table: "ChannelRewards"
            );

            migrationBuilder.DropColumn(
                name: "RewardCode",
                schema: "twitch",
                table: "ChannelRewards"
            );
        }
    }
}
