using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace MARS.TestKit.Postgres;

/// <summary>
/// Фабрика контекста поверх живой PostgreSQL: своя база на экземпляр и
/// настоящие миграции сервиса вместо <c>EnsureCreated</c>.
///
/// Зачем именно PostgreSQL, а не InMemory или SQLite:
/// <list type="bullet">
/// <item>InMemory не переводит запросы, доступные только relational-провайдеру —
///       <c>ExecuteUpdateAsync</c> и <c>EF.Functions.ILike</c> бросали бы
///       исключение, и тест проверял бы провайдер, а не код;</item>
/// <item>SQLite — другой диалект: у него другие правила уникальности, пустые
///       строки в датах, отсутствие <c>timestamptz</c>, и запрос с
///       <c>ILIKE</c> не переводится вовсе;</item>
/// <item><c>EnsureCreated</c> строит схему из модели, а не из миграций, поэтому
///       расхождение с production-схемой осталось бы незамеченным: тест зелёный,
///       а развёртывание падает на <c>RunMarsSchemaMigrationsAsync</c>.</item>
/// </list>
///
/// База создаётся лениво: тесты, которым БД не нужна, не платят за неё, а
/// синхронный <see cref="IDbContextFactory{TContext}.CreateDbContext"/> остаётся
/// рабочим — вызывающий код сервисов не ждёт асинхронности.
/// </summary>
public abstract class PostgresTestDbContextFactory<TContext>
    : IDbContextFactory<TContext>,
        IDisposable,
        IAsyncDisposable
    where TContext : DbContext
{
    private static readonly SemaphoreSlim TemplateGate = new(1, 1);

    /// <summary>
    /// Шаблон на тип контекста: миграции применяются в нём один раз на процесс, а
    /// база теста клонируется из него. <c>CREATE DATABASE ... TEMPLATE</c> копирует
    /// файлы, и это заметно дешевле повторного прогона миграций для каждого теста.
    /// </summary>
    private static readonly Dictionary<Type, string> Templates = [];

    private readonly Lazy<Task<string>> _connectionString;
    private DbContextOptions<TContext>? _options;
    private bool _disposed;

    protected PostgresTestDbContextFactory()
    {
        DatabaseName = $"mars_test_{typeof(TContext).Name.ToLowerInvariant()}_{Guid.NewGuid():N}"[
            ..40
        ];
        _connectionString = new Lazy<Task<string>>(
            CreateDatabaseAsync,
            LazyThreadSafetyMode.ExecutionAndPublication
        );
    }

    /// <summary>Имя базы теста: нужно для диагностики, если тест упал.</summary>
    public string DatabaseName { get; }

    public TContext CreateDbContext() => CreateContext(GetOptions());

    public async Task<TContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
    {
        return CreateContext(await GetOptionsAsync(cancellationToken));
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;

        if (_connectionString.IsValueCreated)
        {
            // Пул соединений держит открытые подключения к базе теста, и без их
            // обрыва DROP DATABASE не проходит.
            NpgsqlConnection.ClearAllPools();
            await MarsPostgres.DropDatabaseAsync(DatabaseName);
        }
    }

    /// <summary>
    /// Синхронный вариант для тестов, которые освобождают фабрику через
    /// <c>using</c>. Ждёт того же удаления базы, поэтому база не остаётся висеть
    /// до конца прогона.
    /// </summary>
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    protected virtual void DisposeCore()
    {
        // Обрыв пулов нужен и синхронному Dispose: без него удалить базу нельзя.
        NpgsqlConnection.ClearAllPools();
    }

    private DbContextOptions<TContext> GetOptions() =>
        GetOptionsAsync(CancellationToken.None).GetAwaiter().GetResult();

    private async Task<DbContextOptions<TContext>> GetOptionsAsync(
        CancellationToken cancellationToken
    )
    {
        var connectionString = await _connectionString.Value.WaitAsync(cancellationToken);
        return _options ??= BuildOptions(connectionString);
    }

    private async Task<string> CreateDatabaseAsync()
    {
        var template = await EnsureTemplateAsync();
        var adminConnectionString = await MarsPostgres.GetAdminConnectionStringAsync();
        await using var connection = new NpgsqlConnection(adminConnectionString);
        await connection.OpenAsync();

        await using var command = connection.CreateCommand();

        // Клонирование шаблона требует, чтобы к нему не было открытых соединений.
        // Пул закрывает соединение лениво, поэтому перед копированием его нужно
        // сбросить — иначе CREATE DATABASE падает с «source database is being
        // accessed by other users».
        NpgsqlConnection.ClearAllPools();
        command.CommandText = $"CREATE DATABASE \"{DatabaseName}\" TEMPLATE \"{template}\"";

        try
        {
            await command.ExecuteNonQueryAsync();
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ObjectInUse)
        {
            // Гонка за шаблоном: кто-то ещё держит на нём соединение. Путь без
            // шаблона медленнее, но всегда работает.
            command.CommandText = $"CREATE DATABASE \"{DatabaseName}\"";
            await command.ExecuteNonQueryAsync();
            await ApplySchemaAsync();
        }

        return ReplaceDatabaseName(adminConnectionString, DatabaseName);
    }

    private async Task ApplySchemaAsync()
    {
        var connectionString = ReplaceDatabaseName(
            await MarsPostgres.GetAdminConnectionStringAsync(),
            DatabaseName
        );

        await using var context = CreateContext(connectionString);
        if (HasMigrations(context))
        {
            await context.Database.MigrateAsync();
        }
        else
        {
            // Контексты без миграций — тестовые пробы в MARS.Shared: им нужна
            // собранная из модели схема, а не её отличие от production.
            await context.Database.EnsureCreatedAsync();
        }
    }

    private async Task<string> EnsureTemplateAsync()
    {
        var contextType = typeof(TContext);

        lock (Templates)
        {
            if (Templates.TryGetValue(contextType, out var existing))
            {
                return existing;
            }
        }

        await TemplateGate.WaitAsync();
        try
        {
            lock (Templates)
            {
                if (Templates.TryGetValue(contextType, out var existing))
                {
                    return existing;
                }
            }

            var templateName = TruncateIdentifier(
                $"mars_test_tmpl_{contextType.Name.ToLowerInvariant()}"
            );
            var adminConnectionString = await MarsPostgres.GetAdminConnectionStringAsync();

            await using (var connection = new NpgsqlConnection(adminConnectionString))
            {
                await connection.OpenAsync();
                await using var drop = connection.CreateCommand();
                drop.CommandText = $"DROP DATABASE IF EXISTS \"{templateName}\" WITH (FORCE)";
                await drop.ExecuteNonQueryAsync();

                await using var create = connection.CreateCommand();
                create.CommandText = $"CREATE DATABASE \"{templateName}\"";
                await create.ExecuteNonQueryAsync();
            }

            var templateConnectionString = ReplaceDatabaseName(adminConnectionString, templateName);
            await using (var context = CreateContext(templateConnectionString))
            {
                if (HasMigrations(context))
                {
                    await context.Database.MigrateAsync();
                }
                else
                {
                    await context.Database.EnsureCreatedAsync();
                }
            }

            // Шаблон клонируется, а к нему подключаться больше нельзя.
            NpgsqlConnection.ClearAllPools();

            lock (Templates)
            {
                Templates[contextType] = templateName;
            }

            return templateName;
        }
        finally
        {
            TemplateGate.Release();
        }
    }

    private static TContext CreateContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<TContext>().UseNpgsql(connectionString).Options;
        return CreateContext(options);
    }

    /// <summary>
    /// Контекст создаётся рефлексией: у <see cref="DbContext"/> нет ограничения
    /// <c>new()</c>, а принимать фабрику типов в конструктор каждого теста было бы
    /// лишней работой во всех тестовых проектах.
    /// </summary>
    private static TContext CreateContext(DbContextOptions<TContext> options)
    {
        return (TContext)Activator.CreateInstance(typeof(TContext), options)!;
    }

    private static DbContextOptions<TContext> BuildOptions(string connectionString)
    {
        return new DbContextOptionsBuilder<TContext>().UseNpgsql(connectionString).Options;
    }

    private static bool HasMigrations(TContext context)
    {
        return context.Database.GetMigrations().Any();
    }

    /// <summary>
    /// PostgreSQL обрезает имя объекта до 63 байт. Обрезка нужна и без неё:
    /// длинное имя упало бы с ошибкой, а короткое — с неверным срезом.
    /// </summary>
    private static string TruncateIdentifier(string name)
    {
        const int maxIdentifierLength = 63;
        return name.Length <= maxIdentifierLength ? name : name[..maxIdentifierLength];
    }

    private static string ReplaceDatabaseName(string connectionString, string databaseName)
    {
        return new NpgsqlConnectionStringBuilder(connectionString)
        {
            Database = databaseName,
        }.ConnectionString;
    }
}
