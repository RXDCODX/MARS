using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Twitch.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropTwitchHusbandsFumoUsersRollCooldowns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Husbands", schema: "twitch");
            migrationBuilder.DropTable(name: "FumoUsers", schema: "twitch");
            migrationBuilder.DropTable(name: "RollCooldowns", schema: "twitch");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder) { }
    }
}
