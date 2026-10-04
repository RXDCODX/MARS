using Npgsql;

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
///
/// Контейнер удаляет <see cref="PostgresContainerScope"/>: по выходе из процесса
/// и при явном <see cref="DisposeContainerAsync"/>. Ryuk (resource reaper) — только
/// страховка: он может не стартовать, и тогда «Ryuk сам уберёт» оставит контейнер
/// в Docker навсегда.
/// </summary>
public static class MarsPostgres
{
    private static readonly SemaphoreSlim Gate = new(1, 1);

    private static PostgresContainerScope? _scope;
    private static string? _adminConnectionString;

    static MarsPostgres()
    {
        // Выход из процесса — точка «после выполнения тестов»: тут контейнер
        // удаляется сам, без Ryuk. Обработчик синхронный, поэтому удаление
        // блокирующее, а его предел задан в PostgresContainerScope.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => DisposeContainerOnExit();
    }

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

    /// <summary>
    /// Удаляет общий контейнер и забывает о нём: следующая база поднимет новый.
    ///
    /// Токена нет намеренно. Вызывающий код, который удаляет ресурсы, работает уже
    /// после отмены теста, и удаление под отменённым токеном
    /// <c>TestContext.Current</c> не выполнилось бы — контейнер остался бы именно в
    /// том прогоне, который разбирают.
    /// </summary>
    public static async Task DisposeContainerAsync()
    {
        PostgresContainerScope? scope = null;

        await Gate.WaitAsync(CancellationToken.None);
        try
        {
            scope = _scope;
            _scope = null;
            _adminConnectionString = null;
        }
        finally
        {
            Gate.Release();
        }

        if (scope is not null)
        {
            await scope.DisposeAsync();
        }
    }

    private static async Task<string> StartContainerAsync(CancellationToken cancellationToken)
    {
        var scope = await PostgresContainerScope.StartAsync(cancellationToken);

        _scope = scope;
        return scope.AdminConnectionString;
    }

    private static void DisposeContainerOnExit()
    {
        try
        {
            DisposeContainerAsync().GetAwaiter().GetResult();
        }
        catch
        {
            // Процесс уже уходит, и Ryuk для этого случая остаётся страховкой:
            // сообщение об ошибке удаления здесь никто не прочитает.
        }
    }
}
