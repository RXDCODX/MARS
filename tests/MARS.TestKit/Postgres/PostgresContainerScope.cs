using Testcontainers.PostgreSql;

namespace MARS.TestKit.Postgres;

/// <summary>
/// Контейнер PostgreSQL, поднятый тестом, и удаляющийся вместе с ним.
///
/// Отдельный тип, а не статическое поле в <see cref="MarsPostgres"/>, по двум
/// причинам. Удаление должно быть тем же кодом, который поднял контейнер, —
/// иначе остаётся надежда на Ryuk, а он может не стартовать. И проверять это
/// удаление можно, не ломая соседние тесты: контейнер теста принадлежит тесту,
/// а общий контейнер процесса трогать посреди прогона нельзя.
/// </summary>
public sealed class PostgresContainerScope : IDisposable, IAsyncDisposable
{
    /// <summary>
    /// Предел ожидания удаления. Docker на этой машине отвечает за доли секунды;
    /// ограничение нужно, чтобы зависший демон не подвесил прогон.
    /// </summary>
    private static readonly TimeSpan DeleteTimeout = TimeSpan.FromSeconds(30);

    private readonly PostgreSqlContainer _container;
    private int _disposed;

    private PostgresContainerScope(PostgreSqlContainer container)
    {
        _container = container;
    }

    /// <summary>
    /// Строка подключения к служебной базе контейнера: ею создаются и удаляются
    /// базы тестов.
    /// </summary>
    public string AdminConnectionString => _container.GetConnectionString();

    /// <summary>
    /// Поднимает контейнер и ждёт его готовности.
    /// </summary>
    public static async Task<PostgresContainerScope> StartAsync(
        CancellationToken cancellationToken = default
    )
    {
        // Образ закреплён на версию из docker-compose стенда: проверка миграций на
        // другой версии PostgreSQL проверяла бы не тот сервер, на котором сервисы
        // работают.
        var container = new PostgreSqlBuilder("postgres:16")
            .WithDatabase("mars_test_admin")
            .WithUsername("mars_test")
            .WithPassword("mars_test")
            // Данные postgres — в tmpfs, а не в томе. Образ объявляет VOLUME
            // /var/lib/postgresql/data, и этот том создаёт демон ДО контейнера,
            // поэтому в нём нет меток org.testcontainers.*: Ryuk удаляет ресурсы по
            // меткам и такой том не видит, он остаётся висящим после каждого
            // прогона. На tmpfs монтирования нет вовсе — проверено: postgres
            // стартует и отвечает, список висящих томов не растёт.
            .WithTmpfsMount("/var/lib/postgresql/data")
            .Build();

        await container.StartAsync(cancellationToken);

        return new PostgresContainerScope(container);
    }

    /// <summary>
    /// Останавливает и удаляет контейнер, дожидаясь удаления: остановленный, но не
    /// удалённый контейнер остаётся в <c>docker ps -a</c> и держит имя.
    ///
    /// Ryuk после этого не нужен: он остаётся страховкой на случай, когда процесс
    /// убит до <see cref="DisposeAsync"/>, и не основанием для удаления.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 0)
        {
            using var timeout = new CancellationTokenSource(DeleteTimeout);

            await _container.DisposeAsync().AsTask().WaitAsync(timeout.Token);
        }
    }

    /// <summary>
    /// Синхронный вариант для фикстур, освобождающихся через <c>using</c>.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }
}
