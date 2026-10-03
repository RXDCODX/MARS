using MARS.CinemaQueue.Data;
using MARS.TestKit.Postgres;

namespace MARS.CinemaQueue.Tests;

/// <summary>
/// Фабрика контекстов поверх живой PostgreSQL: своя база на тест и схема из
/// миграций. Нужна репозиторию и сервису — они создают контекст на каждый вызов,
/// а не держат один на всё время.
/// </summary>
public sealed class TestDbContextFactory : PostgresTestDbContextFactory<CinemaDbContext>;
