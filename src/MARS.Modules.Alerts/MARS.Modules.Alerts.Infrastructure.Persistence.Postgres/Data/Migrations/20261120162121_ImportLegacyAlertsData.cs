using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace MARS.Modules.Alerts.Infrastructure.Persistence.Postgres.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImportLegacyAlertsData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO alerts."AdhdLayoutConfig" ("Id","ShowRainEffect","ShowDVDLogos","ShowBreakingNews","ShowStreamerVideo","ShowFitnessVideo","ShowGTAVideo","ShowHydraulicMobileVideo","ShowSlimeVideo","ShowMukbangVideo","ShowQuiz","ShowSurfer","ShowLOFIGirl","ShowCatisa","ShowNotifications","CreatedAt","UpdatedAt","DvdLogosCount","ShowTimer")
                VALUES ('1','f','f','t','t','t','t','t','t','t','t','t','f','f','t','2026-09-07 19:36:42.887948+04','2026-09-07 21:10:27.701097+04','5','t')
                ON CONFLICT DO NOTHING;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Откат переноса данных не поддерживается: строки — единственная копия legacy-данных."
            );
        }
    }
}
