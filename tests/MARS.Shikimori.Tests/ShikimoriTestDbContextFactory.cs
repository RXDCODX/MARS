using MARS.Shikimori.Data;
using MARS.TestKit.Postgres;

namespace MARS.Shikimori.Tests;

/// <summary>
/// Контекст Shikimori поверх живой PostgreSQL: своя база на тест и схема из
/// миграций.
/// </summary>
internal sealed class ShikimoriTestDbContextFactory
    : PostgresTestDbContextFactory<ShikimoriDbContext>;
