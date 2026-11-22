using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.WaifuGacha.Infrastructure.Postgres.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFumoRollUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FumoRollUsers",
                schema: "waifu",
                columns: table => new
                {
                    TwitchId = table.Column<string>(
                        type: "character varying(50)",
                        maxLength: 50,
                        nullable: false
                    ),
                    LastTime = table.Column<DateTime>(
                        type: "timestamp with time zone",
                        nullable: false
                    ),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FumoRollUsers", x => x.TwitchId);
                }
            );

            migrationBuilder.Sql(
                @"INSERT INTO waifu.""FumoRollUsers"" (""TwitchId"",""LastTime"") VALUES
('124215281','2026-05-15 12:25:27.356673+00'),
('124597564','2026-09-04 17:10:26.568333+00'),
('126931084','2025-04-11 08:02:45.309987+00'),
('138694941','2025-03-28 15:58:14.289985+00'),
('158105504','2026-03-13 08:04:46.013163+00'),
('194086611','2026-05-15 14:20:46.008095+00'),
('195710124','2026-10-30 00:13:46.606613+00'),
('37795951','2026-07-09 22:41:45.795579+00'),
('439056875','2025-06-18 21:40:00.964647+00'),
('47010495','2026-10-16 11:55:08.986337+00'),
('533361038','2025-07-04 17:06:07.121051+00'),
('548278008','2026-09-04 14:29:11.734619+00'),
('723766021','2026-06-25 20:02:25.653101+00'),
('783645904','2026-05-15 14:52:10.220399+00'),
('785975641','2026-10-30 22:36:42.327337+00'),
('854983207','2026-05-29 10:00:12.449405+00'),
('89308929','2026-05-29 09:54:53.300703+00')
ON CONFLICT (""TwitchId"") DO NOTHING;"
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "FumoRollUsers", schema: "waifu");
        }
    }
}
