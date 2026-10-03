using MARS.Admin.Data;
using MARS.TestKit.Postgres;

namespace MARS.Admin.Tests.Services;

/// <summary>
/// Контекст админки поверх живой PostgreSQL: своя база на тест и схема из
/// настоящих миграций.
///
/// Раньше стоял провайдер в памяти, а схему создавал сам провайдер — то есть
/// тест проверял модель, а не то, что миграции админки вообще применимы.
/// </summary>
internal sealed class AdminDbContextFactory : PostgresTestDbContextFactory<AdminDbContext>;
