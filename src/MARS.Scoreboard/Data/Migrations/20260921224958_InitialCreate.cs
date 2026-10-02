using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MARS.Scoreboard.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "scoreboard");

            migrationBuilder.CreateTable(
                name: "ScoreboardStates",
                schema: "scoreboard",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Title = table.Column<string>(type: "text", nullable: false),
                    FightRule = table.Column<string>(type: "text", nullable: false),
                    MainColor = table.Column<string>(type: "text", nullable: false),
                    PlayerNamesColor = table.Column<string>(type: "text", nullable: false),
                    TournamentTitleColor = table.Column<string>(type: "text", nullable: false),
                    FightModeColor = table.Column<string>(type: "text", nullable: false),
                    ScoreColor = table.Column<string>(type: "text", nullable: false),
                    BackgroundColor = table.Column<string>(type: "text", nullable: false),
                    BorderColor = table.Column<string>(type: "text", nullable: false),
                    IsVisible = table.Column<bool>(type: "boolean", nullable: false),
                    AnimationDuration = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreboardStates", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "ScoreboardLayouts",
                schema: "scoreboard",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    HeaderTop = table.Column<int>(type: "integer", nullable: false),
                    HeaderLeft = table.Column<int>(type: "integer", nullable: false),
                    PlayersTop = table.Column<int>(type: "integer", nullable: false),
                    PlayersLeft = table.Column<int>(type: "integer", nullable: false),
                    PlayersRight = table.Column<int>(type: "integer", nullable: false),
                    HeaderHeight = table.Column<int>(type: "integer", nullable: false),
                    HeaderWidth = table.Column<int>(type: "integer", nullable: false),
                    PlayerBarHeight = table.Column<int>(type: "integer", nullable: false),
                    PlayerBarWidth = table.Column<int>(type: "integer", nullable: false),
                    ScoreSize = table.Column<int>(type: "integer", nullable: false),
                    FlagSize = table.Column<int>(type: "integer", nullable: false),
                    Spacing = table.Column<int>(type: "integer", nullable: false),
                    Padding = table.Column<int>(type: "integer", nullable: false),
                    ShowHeader = table.Column<bool>(type: "boolean", nullable: false),
                    ShowFlags = table.Column<bool>(type: "boolean", nullable: false),
                    ShowSponsors = table.Column<bool>(type: "boolean", nullable: false),
                    ShowTags = table.Column<bool>(type: "boolean", nullable: false),
                    ScoreboardStateId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreboardLayouts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreboardLayouts_ScoreboardStates_ScoreboardStateId",
                        column: x => x.ScoreboardStateId,
                        principalSchema: "scoreboard",
                        principalTable: "ScoreboardStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "ScoreboardPlayers",
                schema: "scoreboard",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Sponsor = table.Column<string>(type: "text", nullable: false),
                    Score = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "text", nullable: false),
                    Flag = table.Column<string>(type: "text", nullable: false),
                    Final = table.Column<string>(type: "text", nullable: false),
                    Position = table.Column<int>(type: "integer", nullable: false),
                    ScoreboardStateId = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScoreboardPlayers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScoreboardPlayers_ScoreboardStates_ScoreboardStateId",
                        column: x => x.ScoreboardStateId,
                        principalSchema: "scoreboard",
                        principalTable: "ScoreboardStates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_ScoreboardLayouts_ScoreboardStateId",
                schema: "scoreboard",
                table: "ScoreboardLayouts",
                column: "ScoreboardStateId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_ScoreboardPlayers_ScoreboardStateId",
                schema: "scoreboard",
                table: "ScoreboardPlayers",
                column: "ScoreboardStateId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "ScoreboardLayouts", schema: "scoreboard");

            migrationBuilder.DropTable(name: "ScoreboardPlayers", schema: "scoreboard");

            migrationBuilder.DropTable(name: "ScoreboardStates", schema: "scoreboard");
        }
    }
}
