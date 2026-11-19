using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.WaifuGacha.Infrastructure.Postgres.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMikuMondayTrackMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Artist",
                schema: "waifu",
                table: "MikuMondayTracks",
                type: "text",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "ArtworkUrl",
                schema: "waifu",
                table: "MikuMondayTracks",
                type: "text",
                nullable: true
            );

            migrationBuilder.AddColumn<string>(
                name: "TrackName",
                schema: "waifu",
                table: "MikuMondayTracks",
                type: "text",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.AddColumn<string>(
                name: "Url",
                schema: "waifu",
                table: "MikuMondayTracks",
                type: "text",
                nullable: false,
                defaultValue: ""
            );

            migrationBuilder.Sql(
                """
                DO $$
                BEGIN
                    IF to_regclass('public."SoundRequestBaseTrackInfos"') IS NOT NULL THEN
                        UPDATE waifu."MikuMondayTracks" t
                        SET "TrackName" = COALESCE(s."TrackName", ''),
                            "Artist" = COALESCE(array_to_string(s."Authors", ', '), ''),
                            "Url" = COALESCE(s."Url"::text, ''),
                            "ArtworkUrl" = s."ArtworkUrl"::text
                        FROM public."SoundRequestBaseTrackInfos" s
                        WHERE t."BaseTrackInfoId" = s."Id";
                    END IF;
                END
                $$;
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Artist", schema: "waifu", table: "MikuMondayTracks");

            migrationBuilder.DropColumn(
                name: "ArtworkUrl",
                schema: "waifu",
                table: "MikuMondayTracks"
            );

            migrationBuilder.DropColumn(
                name: "TrackName",
                schema: "waifu",
                table: "MikuMondayTracks"
            );

            migrationBuilder.DropColumn(name: "Url", schema: "waifu", table: "MikuMondayTracks");
        }
    }
}
