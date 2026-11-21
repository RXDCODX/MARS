using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace MARS.CinemaQueue.Infrastructure.Postgres.Data.Migrations;

/// <inheritdoc />
public partial class ImportLegacyCinemaData : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            INSERT INTO cinema."CinemaQueue" ("Id","Title","Description","MediaUrl","Status","Priority","CreatedAt","ScheduledFor","TwitchUserId","Notes","IsNext","LastModified")
            VALUES ('0199590f-dfb6-7d1b-a48a-a1045fce354c','Шрэк (2001)','Жил да был в сказочном государстве большой зеленый великан по имени Шрэк. Жил он в гордом одиночестве в лесу, на болоте, которое считал своим. Но однажды злобный коротышка — лорд Фаркуад, правитель волшебного королевства, безжалостно согнал на Шрэково болото всех сказочных обитателей.,
            И беспечной жизни зеленого великана пришел конец. Но лорд Фаркуад пообещал вернуть Шрэку болото, если великан добудет ему прекрасную принцессу Фиону, которая томится в неприступной башне, охраняемой огнедышащим драконом.','https://www.kinopoisk.ru/film/430/?utm_referrer=yandex.ru','2','0','2025-09-17 23:03:56.595658+04','2025-11-30 23:35:00+04','783645904','Twitch reward redemption - 9/17/2025 11:03:56 PM','f','2025-10-02 23:58:57.415766+04'),
            ('01996882-b812-76c4-bc83-89c615ff1212','Близнецы-драконы (1991)','Два идентичных близнеца были разлучены при рождении и оказались в разных семьях и по разные стороны Тихого океана. Один из близнецов — специалист по боевым искусствам, хулиган и задира, работает автомехаником в гараже отеля, в котором останавливается во время гастролей в Гонконге его брат — знаменитый музыкант всемирно известный дирижер из Нью-Йорка. Когда они встретятся, начнётся невероятная заварушка.','https://www.kinopoisk.ru/film/19273','2','0','2025-09-20 23:03:41.328513+04',NULL,'37795951','Twitch reward redemption - 9/20/2025 11:03:41 PM','f','2025-10-04 22:03:55.269292+04')
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
