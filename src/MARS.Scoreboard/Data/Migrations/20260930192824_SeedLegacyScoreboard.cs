using System;
using Microsoft.EntityFrameworkCore.Migrations;
using MARS.Shared.Data;

#nullable disable

namespace MARS.Scoreboard.Data.Migrations
{
    /// <summary>
    /// Перенос табло, его состояний и участников из staging-схемы <c>public</c>.
    /// </summary>
    /// <remarks>
    /// Порядок копий — <c>ScoreboardStates</c>, затем <c>ScoreboardLayouts</c> и
    /// <c>ScoreboardPlayers</c>: и то и другое ссылается на состояние по внешнему
    /// ключу, поэтому состояние должно попасть в целевую схему первым.
    /// </remarks>
    public partial class SeedLegacyScoreboard : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                LegacyDataSeed.Seed(
                    new[]
                    {
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "ScoreboardStates",
                            TargetSchema: "scoreboard",
                            TargetTable: "ScoreboardStates",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Title",
                                "FightRule",
                                "MainColor",
                                "PlayerNamesColor",
                                "TournamentTitleColor",
                                "FightModeColor",
                                "ScoreColor",
                                "BackgroundColor",
                                "BorderColor",
                                "IsVisible",
                                "AnimationDuration",
                                "CreatedAt",
                                "UpdatedAt",
                                "IsActive")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "ScoreboardLayouts",
                            TargetSchema: "scoreboard",
                            TargetTable: "ScoreboardLayouts",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "HeaderTop",
                                "HeaderLeft",
                                "PlayersTop",
                                "PlayersLeft",
                                "PlayersRight",
                                "HeaderHeight",
                                "HeaderWidth",
                                "PlayerBarHeight",
                                "PlayerBarWidth",
                                "ScoreSize",
                                "FlagSize",
                                "Spacing",
                                "Padding",
                                "ShowHeader",
                                "ShowFlags",
                                "ShowSponsors",
                                "ShowTags",
                                "ScoreboardStateId")
                        ),
                        new LegacyDataSeed.TableCopy(
                            StagingTable: "ScoreboardPlayers",
                            TargetSchema: "scoreboard",
                            TargetTable: "ScoreboardPlayers",
                            Columns: LegacyDataSeed.Columns(
                                "Id",
                                "Name",
                                "Sponsor",
                                "Score",
                                "Tag",
                                "Flag",
                                "Final",
                                "Position",
                                "ScoreboardStateId")
                        ),
                    }
                )
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Откат переноса данных невозможен: staging удаляется на этапе Up, а сами
            // строки — единственная копия legacy-данных. Восстановление только из
            // резервной копии БД.
            throw new NotSupportedException(
                "Откат переноса данных не поддерживается: staging удаляется при применении"
                    + " миграции. Для восстановления разверните резервную копию."
            );
        }
    }
}