using MARS.TestKit.Postgres;
using MARS.WaifuGacha.Infrastructure.Postgres.Data;

namespace MARS.WaifuGacha.Tests;

/// <summary>
/// Контекст WaifuGacha поверх живой PostgreSQL: своя база на тест и настоящие
/// миграции.
/// </summary>
internal sealed class WaifuTestDbContextFactory : PostgresTestDbContextFactory<WaifuDbContext>;
