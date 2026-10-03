using MARS.Scoreboard.Data;
using MARS.TestKit.Postgres;

namespace MARS.Scoreboard.Tests.Grpc;

/// <summary>
/// Фабрика контекстов поверх живой PostgreSQL: своя база на тест и схема из
/// миграций.
/// </summary>
public sealed class TestDbContextFactory : PostgresTestDbContextFactory<ScoreboardDbContext>;
