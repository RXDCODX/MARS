using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.Scoreboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class ImportLegacyScoreboardData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO scoreboard."ScoreboardStates" ("Id","Title","FightRule","MainColor","PlayerNamesColor","TournamentTitleColor","FightModeColor","ScoreColor","BackgroundColor","BorderColor","IsVisible","AnimationDuration","CreatedAt","UpdatedAt","IsActive")
                VALUES (10028,'Street Fighter 6','','#e6ac0c','#ffffff','#e6ac0c','#e6ac0c','#ffffff','#23272f','#e6ac0c',false,800,'2025-09-21 20:23:50.296405+04'::timestamptz,'2026-05-16 14:22:03.97634+04'::timestamptz,true)
                ON CONFLICT DO NOTHING;

                INSERT INTO scoreboard."ScoreboardLayouts" ("Id","HeaderTop","HeaderLeft","PlayersTop","PlayersLeft","PlayersRight","HeaderHeight","HeaderWidth","PlayerBarHeight","PlayerBarWidth","ScoreSize","FlagSize","Spacing","Padding","ShowHeader","ShowFlags","ShowSponsors","ShowTags","ScoreboardStateId")
                VALUES (10027,0,50,20,125,125,40,300,52,550,60,24,16,16,true,true,true,true,10028)
                ON CONFLICT DO NOTHING;

                INSERT INTO scoreboard."ScoreboardPlayers" ("Id","Name","Sponsor","Score","Tag","Flag","Final","Position","ScoreboardStateId")
                VALUES (20055,'RXDCODX','Red Bull',0,'STREAMER','ru','none',1,10028),
                       (20056,'Daigo Umehara','Red Bull',0,'The Beast','jp','none',2,10028)
                ON CONFLICT DO NOTHING;

                SELECT setval(pg_get_serial_sequence('scoreboard."ScoreboardStates"','Id'), COALESCE((SELECT max("Id") FROM scoreboard."ScoreboardStates"),0)+1, false);
                SELECT setval(pg_get_serial_sequence('scoreboard."ScoreboardLayouts"','Id'), COALESCE((SELECT max("Id") FROM scoreboard."ScoreboardLayouts"),0)+1, false);
                SELECT setval(pg_get_serial_sequence('scoreboard."ScoreboardPlayers"','Id'), COALESCE((SELECT max("Id") FROM scoreboard."ScoreboardPlayers"),0)+1, false);
                """
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            throw new NotSupportedException(
                "Откат переноса данных не поддерживается: строки — единственная копия legacy-данных."
            );
        }
    }
}
