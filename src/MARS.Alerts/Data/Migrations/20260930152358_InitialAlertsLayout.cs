using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Alerts.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialAlertsLayout : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "alerts");

            migrationBuilder.CreateTable(
                name: "AdhdLayoutConfig",
                schema: "alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    ShowRainEffect = table.Column<bool>(type: "boolean", nullable: false),
                    ShowDVDLogos = table.Column<bool>(type: "boolean", nullable: false),
                    ShowBreakingNews = table.Column<bool>(type: "boolean", nullable: false),
                    ShowStreamerVideo = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFitnessVideo = table.Column<bool>(type: "boolean", nullable: false),
                    ShowGTAVideo = table.Column<bool>(type: "boolean", nullable: false),
                    ShowHydraulicMobileVideo = table.Column<bool>(type: "boolean", nullable: false),
                    ShowSlimeVideo = table.Column<bool>(type: "boolean", nullable: false),
                    ShowMukbangVideo = table.Column<bool>(type: "boolean", nullable: false),
                    ShowQuiz = table.Column<bool>(type: "boolean", nullable: false),
                    ShowSurfer = table.Column<bool>(type: "boolean", nullable: false),
                    ShowLOFIGirl = table.Column<bool>(type: "boolean", nullable: false),
                    ShowCatisa = table.Column<bool>(type: "boolean", nullable: false),
                    ShowNotifications = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DvdLogosCount = table.Column<int>(type: "integer", nullable: false),
                    ShowTimer = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdhdLayoutConfig", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdhdLayoutConfig",
                schema: "alerts");
        }
    }
}
