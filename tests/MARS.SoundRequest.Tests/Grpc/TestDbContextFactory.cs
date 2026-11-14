using MARS.SoundRequest.Data;
using MARS.TestKit.Postgres;

namespace MARS.SoundRequest.Tests.Grpc;

/// <summary>
/// Фабрика контекстов поверх живой PostgreSQL: своя база на тест и схема из
/// миграций.
/// </summary>
public sealed class TestDbContextFactory : PostgresTestDbContextFactory<MediaDbContext>
{
}
