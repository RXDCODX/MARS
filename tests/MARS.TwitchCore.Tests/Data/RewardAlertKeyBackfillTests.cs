using MARS.Shared.Models.Rewards;
using MARS.TestKit.Postgres;
using Npgsql;

namespace MARS.TwitchCore.Tests.Data;

/// <summary>
/// SQL-бэкфилл миграции <c>AddRewardAlertKey</c> проверяется на живой PostgreSQL.
/// </summary>
/// <remarks>
/// <para>
/// Проверка на строках, а не на регулярке: <c>LIKE</c> в PostgreSQL
/// регистрозависим, «Fumo Friday Night» содержит «Fumo Friday», а «Викторина» и
/// «Аудио викторина» отличаются регистром первой буквы. Ошибка в порядке строк
/// или в регистре видна только при выполнении, и на стенде она проявилась бы уже
/// после миграции, когда ключи проставлены неверно.
/// </para>
/// <para>
/// На <c>AlertKey</c> висит уникальный индекс, поэтому скрипт обязан выбрать по
/// одной строке на ключ: дубликат названия в базе уронил бы миграцию целиком.
/// </para>
/// </remarks>
public class RewardAlertKeyBackfillTests : IAsyncLifetime
{
    private static readonly Dictionary<string, string> ExpectedKeys = new(StringComparer.Ordinal)
    {
        ["🎉 Конфетти!"] = RewardAlertKeys.Confetti,
        ["🎆 Фееерверк!"] = RewardAlertKeys.Fireworks,
        ["🎵 Phonk Edit"] = RewardAlertKeys.PhonkEdit,
        ["🧸 Fumo Friday Night"] = RewardAlertKeys.FumoFridayNight,
        ["🌙 Fumo Friday"] = RewardAlertKeys.FumoFriday,
        ["🎤 Miku Monday Alert"] = RewardAlertKeys.MikuMonday,
        ["🎤 Miku Monday [BETA TEST]"] = RewardAlertKeys.MikuMondayTracks,
        ["🎺 ШКИБИДИБОП"] = RewardAlertKeys.Skibidibop,
        ["🎺 SKIBIDIBOP LONG"] = RewardAlertKeys.SkibidibopLong,
        ["🎵 Аудио викторина"] = RewardAlertKeys.AudioQuiz,
        ["🧠 Викторина"] = RewardAlertKeys.Quiz,
        ["🔍 Поиск супруга"] = RewardAlertKeys.WaifuShow,
        ["👰 Добавить супруга"] = RewardAlertKeys.WaifuAdd,
        ["💒 Свадьба"] = RewardAlertKeys.WaifuMerge,
        ["👋 Привет"] = RewardAlertKeys.Hello,
        ["👋 Пока"] = RewardAlertKeys.Bye,
    };

    private const string DuplicateTitle = "🎉 Конфетти!";
    private const string UnknownTitle = "😀 Неизвестная награда";

    private readonly string _databaseName = "reward_alert_key_" + Guid.NewGuid().ToString("N");

    private string _connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        // Токен теста здесь недоступен: фикстура создаётся до его появления.
        _connectionString = await MarsPostgres.CreateDatabaseAsync(
            _databaseName,
            CancellationToken.None
        );
    }

    public async ValueTask DisposeAsync()
    {
        // Пул держит соединение, и DROP DATABASE без сброса пула не выполнится.
        // Токен — None: удаление идёт и после отменённого прогона.
        NpgsqlConnection.ClearAllPools();
        await MarsPostgres.DropDatabaseAsync(_databaseName, CancellationToken.None);
    }

    [Fact]
    public async Task Скрипт_Проставляет_Ключи_По_Старым_Названиям()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        await ExecuteAsync(connection, CreateSchemaSql, cancellationToken);
        await InsertLegacyRewardsAsync(connection, cancellationToken);

        await ExecuteAsync(connection, LegacyAlertKeySql.Text, cancellationToken);

        var rows = await ReadRowsAsync(connection, cancellationToken);

        foreach (var (title, expected) in ExpectedKeys)
        {
            if (title == DuplicateTitle)
            {
                continue;
            }

            var actual = rows.Where(row => row.Title == title)
                .Select(row => row.AlertKey)
                .ToArray();

            Assert.Equal([expected], actual);
        }

        var duplicates = rows.Where(row => row.Title == DuplicateTitle).ToArray();

        Assert.Equal(2, duplicates.Length);
        Assert.Single(duplicates, row => row.AlertKey == RewardAlertKeys.Confetti);
        Assert.Single(duplicates, row => row.AlertKey is null);

        Assert.Null(rows.Single(row => row.Title == UnknownTitle).AlertKey);
    }

    private const string CreateSchemaSql = """
        CREATE SCHEMA twitch;
        CREATE TABLE twitch."ChannelRewards" (
            "Id" uuid NOT NULL PRIMARY KEY,
            "Order" integer NOT NULL,
            "Title" text NOT NULL,
            "IsDeleted" boolean NOT NULL DEFAULT false,
            "AlertKey" character varying(64) NULL
        );
        CREATE UNIQUE INDEX "IX_ChannelRewards_AlertKey"
            ON twitch."ChannelRewards" ("AlertKey")
            WHERE 'AlertKey' IS NOT NULL;
        """;

    private static async Task InsertLegacyRewardsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        var titles = ExpectedKeys
            .Keys.Append(DuplicateTitle)
            .Append(UnknownTitle)
            .Select((title, index) => (Title: title, Order: index))
            .ToArray();

        foreach (var (title, order) in titles)
        {
            await using var command = connection.CreateCommand();
            command.CommandText = """
                INSERT INTO twitch."ChannelRewards" ("Id", "Order", "Title")
                VALUES (@id, @order, @title);
                """;
            command.Parameters.AddWithValue("id", Guid.NewGuid());
            command.Parameters.AddWithValue("order", order);
            command.Parameters.AddWithValue("title", title);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static async Task<(string Title, string? AlertKey)[]> ReadRowsAsync(
        NpgsqlConnection connection,
        CancellationToken cancellationToken
    )
    {
        var rows = new List<(string, string?)>();

        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT "Title", "AlertKey" FROM twitch."ChannelRewards" ORDER BY "Order";
            """;
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            rows.Add((reader.GetString(0), reader.IsDBNull(1) ? null : reader.GetString(1)));
        }

        return rows.ToArray();
    }

    private static async Task ExecuteAsync(
        NpgsqlConnection connection,
        string sql,
        CancellationToken cancellationToken
    )
    {
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}
