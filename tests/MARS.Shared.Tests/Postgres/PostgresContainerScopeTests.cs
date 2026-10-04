using System.Net.Sockets;
using MARS.TestKit.Postgres;
using Npgsql;

namespace MARS.Shared.Tests.Postgres;

/// <summary>
/// Контейнер тестовой базы удаляет тот же код, который его поднял.
///
/// Ryuk (resource reaper) для этого не годится: на этой машине он остался в
/// <c>docker ps -a</c> в состоянии <c>Created</c>, то есть сборщик мусора не
/// отработал ни разу, и контейнеры за ним остались. Поэтому удаление обязано
/// быть в коде фикстуры, а Ryuk — только страховка.
/// </summary>
public class PostgresContainerScopeTests
{
    /// <summary>
    /// После удаления контейнера база по его старой строке подключения больше не
    /// отвечает, а повторное удаление ничего не делает: иначе фикстура, вызванная
    /// дважды, роняла бы прогон.
    ///
    /// Контейнер здесь свой, а не общий <see cref="MarsPostgres"/>: удаление общего
    /// контейнера посреди прогона уронило бы параллельные тесты, которые в него
    /// уже ходят.
    /// </summary>
    [Fact]
    public async Task DisposeAsyncRemovesContainerAndIgnoresSecondCall()
    {
        var connectionString = string.Empty;
        var scope = await PostgresContainerScope.StartAsync(TestContext.Current.CancellationToken);

        try
        {
            connectionString = scope.AdminConnectionString;
            await AssertPostgresAnswersAsync(connectionString);

            await scope.DisposeAsync();

            // Пул Npgsql переживает удаление контейнера: соединение, отданное в
            // пул, отдаётся снова без проверки, и OpenAsync проходит успешно при
            // мёртвом сервере. Без сброса пула проверка «сервер исчез» врала бы в
            // сторону «жив».
            NpgsqlConnection.ClearAllPools();

            Assert.False(
                await AnswersAsync(connectionString),
                "Контейнер удалён, но postgres по его старой строке подключения ещё отвечает."
            );
            await scope.DisposeAsync();
        }
        finally
        {
            await scope.DisposeAsync();
        }
    }

    /// <summary>
    /// Обязательный шаг теста: без соединения «контейнер удалён» ничего не значит —
    /// удалить можно и не поднятый контейнер.
    /// </summary>
    private static async Task AssertPostgresAnswersAsync(string connectionString)
    {
        Assert.True(
            await AnswersAsync(connectionString),
            "Контейнер поднят, но postgres по строке подключения не отвечает."
        );
    }

    /// <summary>
    /// Проверка одним Npgsql-соединением, а не <c>Testcontainers</c>-объектом:
    /// тест должен убедиться, что сервер исчез, а не что библиотека забыла о нём.
    /// </summary>
    private static async Task<bool> AnswersAsync(string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Timeout = 3 };

        try
        {
            await using var connection = new NpgsqlConnection(builder.ConnectionString);
            await connection.OpenAsync(TestContext.Current.CancellationToken);
            return true;
        }
        catch (Exception exception)
            when (exception is NpgsqlException or SocketException or TimeoutException)
        {
            return false;
        }
    }
}
