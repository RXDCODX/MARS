using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MARS.TwitchCore.Data.Migrations;

/// <inheritdoc />
public partial class AddRewardAlertKey : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "AlertKey",
            schema: "twitch",
            table: "ChannelRewards",
            type: "character varying(64)",
            maxLength: 64,
            nullable: true
        );

        migrationBuilder.AddColumn<bool>(
            name: "IsAlertEnabled",
            schema: "twitch",
            table: "ChannelRewards",
            type: "boolean",
            nullable: false,
            // Уже существующие награды обязаны остаться включёнными: false
            // здесь выключило бы все алерты стенда после миграции.
            defaultValue: true
        );

        migrationBuilder.CreateIndex(
            name: "IX_ChannelRewards_AlertKey",
            schema: "twitch",
            table: "ChannelRewards",
            column: "AlertKey",
            unique: true,
            filter: "'AlertKey' IS NOT NULL"
        );

        // Заполнение ключа по старым названиям из монолита. Идемпотентно:
        // строки с уже проставленным ключом не трогаются, повторный запуск
        // ничего не меняет. Названия — единственное место, где в репозитории
        // остаются эмодзи-строки, поэтому список продублирован в тесте
        // (RewardAlertKeyMigrationTests), который сверяет его с RewardAlertKeys.
        migrationBuilder.Sql(LegacyAlertKeyBackfillSql);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_ChannelRewards_AlertKey",
            schema: "twitch",
            table: "ChannelRewards"
        );

        migrationBuilder.DropColumn(name: "AlertKey", schema: "twitch", table: "ChannelRewards");

        migrationBuilder.DropColumn(
            name: "IsAlertEnabled",
            schema: "twitch",
            table: "ChannelRewards"
        );
    }

    /// <summary>
    /// Старые названия наград из монолита → ключ алерта.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Названия — единственное место в репозитории, где остаются эмодзи-строки,
    /// и второй раз они встречаются в тесте
    /// <c>RewardAlertKeyMigrationTests</c>: он проверяет, что каждый ключ из
    /// этого списка есть в <c>RewardAlertKeys</c>, а ключи с совпадающими
    /// фрагментами названий разбираются по приоритету.
    /// </para>
    /// <para>
    /// <c>LIKE</c> в PostgreSQL регистрозависим, поэтому «Викторина» (награда
    /// <c>quiz</c>) и «Аудио викторина» (<c>audioquiz</c>) не склеиваются, хотя
    /// отличаются только регистром первой буквы. Порядок важен там, где одно
    /// название является частью другого: «Fumo Friday Night» раньше «Fumo
    /// Friday», «SKIBIDIBOP LONG» раньше «ШКИБИДИБОП», «Miku Monday Alert»
    /// раньше «Miku Monday [BETA TEST]».
    /// </para>
    /// <para>
    /// Обновление идемпотентно: трогаются только строки с пустым ключом.
    /// На каждый ключ выбирается одна строка (сначала живые, потом по <c>Id</c>),
    /// потому что на <c>AlertKey</c> висит уникальный индекс — дубликат названия
    /// в базе уронил бы миграцию, а не оставил её частично применённой.
    /// </para>
    /// </remarks>
    private const string LegacyAlertKeyBackfillSql = """
        WITH mapping("Priority", "AlertKey", "Pattern") AS (
            VALUES
                (1,  'fumofridaynight',  '%Fumo Friday Night%'),
                (2,  'fumofriday',       '%🌙 Fumo Friday%'),
                (3,  'mikumonday',       '%Miku Monday Alert%'),
                (4,  'mikumondaytracks', '%Miku Monday [BETA TEST]%'),
                (5,  'skibidiboplong',   '%SKIBIDIBOP LONG%'),
                (6,  'skibidibop',       '%ШКИБИДИБОП%'),
                (7,  'audioquiz',        '%Аудио викторина%'),
                (8,  'quiz',             '%Викторина%'),
                (9,  'confetti',         '%Конфетти%'),
                (10, 'fireworks',        '%Феерверк%'),
                (11, 'fireworks',        '%Фееерверк%'),
                (12, 'phonkedit',        '%Phonk Edit%'),
                (13, 'tiktokedit',       '%Make a TikTok Edit%'),
                (14, 'randomart',        '%Random Art%'),
                (15, 'mikumikubeam',     '%MIKU MIKU BEAM%'),
                (16, 'gaoalert',         '%GAO Alert%'),
                (17, 'what',             '%WHAT%'),
                (18, 'pedro',            '%PEDRO PEDRO PEDRO%'),
                (19, 'tyazhelo',         '%Тяжело%'),
                (20, 'michaeltime',      '%Michael Time%'),
                (21, 'legbum',           '%НОГОЙ БОМЖА%'),
                (22, 'stone',            '%Камень%'),
                (23, 'cringe',           '%CRINGE%'),
                (24, 'hello',            '%👋 Привет%'),
                (25, 'bye',              '%👋 Пока%'),
                (26, 'intelligence',     '%Интеллект%'),
                (27, 'mikuscreamer',     '%MIKU SCREAMER%'),
                (28, 'aga',              '%AGA%'),
                (29, 'badtobone',        '%BAD TO THE BONE%'),
                (30, 'dancedance',       '%DANCE DANCE DANCE%'),
                (31, 'wednsdayfrog',     '%Жабья среда%'),
                (32, 'edge0100alert',    '%Edge0100%'),
                (33, 'statusquestion',   '%STATUS?%'),
                (34, 'cinemarequest',    '%CINEMA/ANIME REQUEST%'),
                (35, 'adhdsuperpower',   '%ADHD Superpower%'),
                (36, 'closegame',        '%Выключить игру%'),
                (37, 'selectgame',       '%Выберите мне игру%'),
                (38, 'credits',          '%🎬 Credits%'),
                (39, 'allrefund',        '%Алармагеддон%'),
                (40, 'randomsound',      '%Random sound%'),
                (41, 'randommem',        '%Random Mem%'),
                (42, 'russianroulette',  '%Русская рулетка%'),
                (43, 'randomreward',     '%Случайная награда%'),
                (44, 'waifuadd',         '%Добавить супруга%'),
                (45, 'waifumerge',       '%Свадьба%'),
                (46, 'waifushow',        '%Поиск супруга%'),
                (47, 'frogroll',         '%Frog Roulette%'),
                (48, 'fumoroll',         '%Fumo Roulette%'),
                (49, 'mikuroll',         '%Мику Рулетка%'),
                (50, 'emojis',           '%Эмодзи%')
        ),
        candidates AS (
            SELECT
                r."Id",
                m."AlertKey",
                ROW_NUMBER() OVER (
                    PARTITION BY r."Id"
                    ORDER BY m."Priority"
                ) AS "MappingRank",
                ROW_NUMBER() OVER (
                    PARTITION BY m."AlertKey"
                    ORDER BY r."IsDeleted", r."Id"
                ) AS "KeyRank"
            FROM twitch."ChannelRewards" AS r
            JOIN mapping AS m ON r."Title" LIKE m."Pattern"
            WHERE r."AlertKey" IS NULL
        )
        UPDATE twitch."ChannelRewards" AS r
        SET "AlertKey" = c."AlertKey"
        FROM candidates AS c
        WHERE r."Id" = c."Id"
          AND c."MappingRank" = 1
          AND c."KeyRank" = 1;
        """;
}
