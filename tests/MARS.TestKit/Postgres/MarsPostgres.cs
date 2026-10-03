using DotNet.Testcontainers.Builders;
using Npgsql;
using Testcontainers.PostgreSql;

namespace MARS.TestKit.Postgres;

/// <summary>
/// PostgreSQL для тестов: один контейнер на процесс прогона, база на тест внутри
/// него.
///
/// Отдельный контейнер на каждый тест не подходит: подъём postgres занимает
/// секунды, и сотня тестов превратилась бы в десятки минут ожидания. Отдельная
/// база на тест нужна, чтобы тесты не видели данные друг друга — иначе первый
/// упавший тест испортил бы весь класс, а прогон был бы неповторяемым.
///
/// Контейнер поднимается лениво и один раз на процесс: поднимается только когда
/// какой-то тест действительно пошёл в базу, иначе тестовые проекты без БД не
/// платили бы за Docker. Тесты идут по одному процессу на проект, поэтому одного
/// контейнера достаточно и при параллельном выполнении классов.
/// </summary>
public static class MarsPostgres
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static PostgreSqlContainer? _container;
    private static string? _adminConnectionString;

    /// <summary>
    /// Строка подключения к служебной базе контейнера: ею создаются и удаляются
    /// базы тестов.
    /// </summary>
    public static async Task<string> GetAdminConnectionStringAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (_adminConnectionString is not null)
        {
            return _adminConnectionString;
        }

        await Gate.WaitAsync(cancellationToken);
        try
        {
            if (_adminConnectionString is null)
            {
                _adminConnectionString = await StartContainerAsync(cancellationToken);
            }
        }
        finally
        {
            Gate.Release();
        }

        return _adminConnectionString;
    }

    /// <summary>
    /// Дожидается поднятия контейнера, не создавая базу. Вызывается
    /// конструктором фабрики: старт контейнера — несколько секунд, и внутри теста
    /// он съедал окно ожидания.
    /// </summary>
    public static Task EnsureStartedAsync(CancellationToken cancellationToken = default) =>
        GetAdminConnectionStringAsync(cancellationToken);

    /// <summary>
    /// Создаёт пустую базу и возвращает строку подключения к ней.
    /// </summary>
    public static async Task<string> CreateDatabaseAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var adminConnectionString = await GetAdminConnectionStringAsync(cancellationToken);
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"CREATE DATABASE \"{name}\"";
        await command.ExecuteNonQueryAsync(cancellationToken);

        return new NpgsqlConnectionStringBuilder(adminConnectionString)
        {
            Database = name,
        }.ConnectionString;
    }

    /// <summary>
    /// Удаляет базу теста. Оставшиеся соединения обрываются: тест мог упасть,
    /// не закрыв контекст, и из-за него база осталась бы висеть до конца прогона.
    /// </summary>
    public static async Task DropDatabaseAsync(
        string name,
        CancellationToken cancellationToken = default
    )
    {
        var adminConnectionString = await GetAdminConnectionStringAsync(cancellationToken);
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"DROP DATABASE IF EXISTS \"{name}\" WITH (FORCE)";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static async Task<string> StartContainerAsync(CancellationToken cancellationToken)
    {
        // Образ закреплён на версию из docker-compose стенда: проверка миграций на
        // другой версии PostgreSQL проверяла бы не тот сервер, на котором сервисы
        // работают.
        var container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("mars_test_admin")
            .WithUsername("mars_test")
            .WithPassword("mars_test")
            .Build();

        await container.StartAsync(cancellationToken);

        _container = container;
        return container.GetConnectionString();
    }
}
