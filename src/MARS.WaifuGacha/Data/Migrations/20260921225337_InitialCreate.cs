using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace MARS.WaifuGacha.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(name: "waifu");

            migrationBuilder.CreateTable(
                name: "AutoHelloMessages",
                schema: "waifu",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    Text = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    Order = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AutoHelloMessages", x => x.Guid);
                }
            );

            migrationBuilder.CreateTable(
                name: "Frogs",
                schema: "waifu",
                columns: table => new
                {
                    Pid = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    CommonName = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    ScientificName = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Family = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    RussianName = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    ThumbnailUrl = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Size = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: true
                    ),
                    Status = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    Category = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: true
                    ),
                    Habits = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    WhenAdded = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastOrder = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Frogs", x => x.Pid);
                }
            );

            migrationBuilder.CreateTable(
                name: "Fumos",
                schema: "waifu",
                columns: table => new
                {
                    MfcId = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Name = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    Character = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    CharacterTranslit = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    Origin = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: true
                    ),
                    ThumbnailUrl = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Rating = table.Column<double>(type: "double precision", nullable: false),
                    RatingCount = table.Column<int>(type: "integer", nullable: false),
                    WhenAdded = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastOrder = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Fumos", x => x.MfcId);
                }
            );

            migrationBuilder.CreateTable(
                name: "Husbands",
                schema: "waifu",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "text", nullable: false),
                    WhenOrdered = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    WaifuBrideId = table.Column<string>(type: "text", nullable: true),
                    IsPrivated = table.Column<bool>(type: "boolean", nullable: false),
                    OrderCount = table.Column<long>(type: "bigint", nullable: false),
                    WaifuRollId = table.Column<string>(type: "text", nullable: true),
                    WhenPrivated = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: true
                    ),
                    LastWeddingCongratulatedMonths = table.Column<int>(
                        type: "integer",
                        nullable: true
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Husbands", x => x.TwitchId);
                }
            );

            migrationBuilder.CreateTable(
                name: "MikuModules",
                schema: "waifu",
                columns: table => new
                {
                    PageId = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Title = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: false
                    ),
                    JapaneseName = table.Column<string>(
                        type: "character varying(300)",
                        maxLength: 300,
                        nullable: true
                    ),
                    Designer = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: true
                    ),
                    ThumbnailUrl = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: false
                    ),
                    Description = table.Column<string>(
                        type: "character varying(2000)",
                        maxLength: 2000,
                        nullable: true
                    ),
                    Songs = table.Column<string>(
                        type: "character varying(500)",
                        maxLength: 500,
                        nullable: true
                    ),
                    WhenAdded = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastOrder = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MikuModules", x => x.PageId);
                }
            );

            migrationBuilder.CreateTable(
                name: "MikuMondayTracks",
                schema: "waifu",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    Number = table.Column<int>(type: "integer", nullable: false),
                    BaseTrackInfoId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MikuMondayTracks", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "RollCooldowns",
                schema: "waifu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TwitchUserId = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    RollType = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    LastRollTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RollCooldowns", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "RootState",
                schema: "waifu",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    TypeDescription = table.Column<string>(type: "text", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RootState", x => x.Name);
                }
            );

            migrationBuilder.CreateTable(
                name: "UserFumoCollections",
                schema: "waifu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TwitchUserId = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    FumoMfcId = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    FirstObtained = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastObtained = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserFumoCollections", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "UserMikuCollections",
                schema: "waifu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TwitchUserId = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    MikuPageId = table.Column<int>(type: "integer", nullable: false),
                    Count = table.Column<int>(type: "integer", nullable: false),
                    FirstObtained = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastObtained = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMikuCollections", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "WaifuRollAudios",
                schema: "waifu",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(
                        type: "character varying(100)",
                        maxLength: 100,
                        nullable: false
                    ),
                    AudioData = table.Column<byte[]>(type: "bytea", nullable: false),
                    FileExtension = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuRollAudios", x => x.Id);
                }
            );

            migrationBuilder.CreateTable(
                name: "WaifuRollGuarantees",
                schema: "waifu",
                columns: table => new
                {
                    TwitchId = table.Column<string>(type: "text", nullable: false),
                    RollCount = table.Column<int>(type: "integer", nullable: false),
                    LastRoll = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    CreatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    UpdatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WaifuRollGuarantees", x => x.TwitchId);
                }
            );

            migrationBuilder.CreateTable(
                name: "HusbandAutoHelloCooldowns",
                schema: "waifu",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    HusbandId = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HusbandAutoHelloCooldowns", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_HusbandAutoHelloCooldowns_Husbands_HusbandId",
                        column: x => x.HusbandId,
                        principalSchema: "waifu",
                        principalTable: "Husbands",
                        principalColumn: "TwitchId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "HusbandCoolDowns",
                schema: "waifu",
                columns: table => new
                {
                    Guid = table.Column<Guid>(type: "uuid", nullable: false),
                    HusbandId = table.Column<string>(type: "text", nullable: false),
                    Time = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HusbandCoolDowns", x => x.Guid);
                    table.ForeignKey(
                        name: "FK_HusbandCoolDowns_Husbands_HusbandId",
                        column: x => x.HusbandId,
                        principalSchema: "waifu",
                        principalTable: "Husbands",
                        principalColumn: "TwitchId",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "MikuMondayActivations",
                schema: "waifu",
                columns: table => new
                {
                    Id = table
                        .Column<int>(type: "integer", nullable: false)
                        .Annotation(
                            "Npgsql:ValueGenerationStrategy",
                            NpgsqlValueGenerationStrategy.IdentityByDefaultColumn
                        ),
                    TwitchUserId = table.Column<string>(type: "text", nullable: false),
                    DisplayName = table.Column<string>(type: "text", nullable: false),
                    MikuMondayTrackId = table.Column<int>(type: "integer", nullable: false),
                    ActivatedAt = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    WeekOfYear = table.Column<int>(type: "integer", nullable: false),
                    Year = table.Column<int>(type: "integer", nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MikuMondayActivations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MikuMondayActivations_MikuMondayTracks_MikuMondayTrackId",
                        column: x => x.MikuMondayTrackId,
                        principalSchema: "waifu",
                        principalTable: "MikuMondayTracks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade
                    );
                }
            );

            migrationBuilder.CreateTable(
                name: "Waifus",
                schema: "waifu",
                columns: table => new
                {
                    ShikiId = table.Column<string>(
                        type: "character varying(20)",
                        maxLength: 20,
                        nullable: false
                    ),
                    Name = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    Age = table.Column<long>(type: "bigint", nullable: false),
                    Anime = table.Column<string>(type: "text", nullable: true),
                    Manga = table.Column<string>(type: "text", nullable: true),
                    WhenAdded = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    LastOrder = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                    OrderCount = table.Column<int>(type: "integer", nullable: false),
                    IsPrivated = table.Column<bool>(type: "boolean", nullable: false),
                    ImageUrl = table.Column<string>(
                        type: "character varying(200)",
                        maxLength: 200,
                        nullable: false
                    ),
                    AudioId = table.Column<Guid>(type: "uuid", nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Waifus", x => x.ShikiId);
                    table.ForeignKey(
                        name: "FK_Waifus_WaifuRollAudios_AudioId",
                        column: x => x.AudioId,
                        principalSchema: "waifu",
                        principalTable: "WaifuRollAudios",
                        principalColumn: "Id"
                    );
                }
            );

            migrationBuilder.CreateIndex(
                name: "IX_HusbandAutoHelloCooldowns_HusbandId",
                schema: "waifu",
                table: "HusbandAutoHelloCooldowns",
                column: "HusbandId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_HusbandCoolDowns_HusbandId",
                schema: "waifu",
                table: "HusbandCoolDowns",
                column: "HusbandId",
                unique: true
            );

            migrationBuilder.CreateIndex(
                name: "IX_MikuMondayActivations_MikuMondayTrackId",
                schema: "waifu",
                table: "MikuMondayActivations",
                column: "MikuMondayTrackId"
            );

            migrationBuilder.CreateIndex(
                name: "IX_Waifus_AudioId",
                schema: "waifu",
                table: "Waifus",
                column: "AudioId"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "AutoHelloMessages", schema: "waifu");

            migrationBuilder.DropTable(name: "Frogs", schema: "waifu");

            migrationBuilder.DropTable(name: "Fumos", schema: "waifu");

            migrationBuilder.DropTable(name: "HusbandAutoHelloCooldowns", schema: "waifu");

            migrationBuilder.DropTable(name: "HusbandCoolDowns", schema: "waifu");

            migrationBuilder.DropTable(name: "MikuModules", schema: "waifu");

            migrationBuilder.DropTable(name: "MikuMondayActivations", schema: "waifu");

            migrationBuilder.DropTable(name: "RollCooldowns", schema: "waifu");

            migrationBuilder.DropTable(name: "RootState", schema: "waifu");

            migrationBuilder.DropTable(name: "UserFumoCollections", schema: "waifu");

            migrationBuilder.DropTable(name: "UserMikuCollections", schema: "waifu");

            migrationBuilder.DropTable(name: "WaifuRollGuarantees", schema: "waifu");

            migrationBuilder.DropTable(name: "Waifus", schema: "waifu");

            migrationBuilder.DropTable(name: "Husbands", schema: "waifu");

            migrationBuilder.DropTable(name: "MikuMondayTracks", schema: "waifu");

            migrationBuilder.DropTable(name: "WaifuRollAudios", schema: "waifu");
        }
    }
}
