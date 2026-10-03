using MARS.TestKit.Postgres;
using MARS.Videos365.Data;

namespace MARS.Videos365.Tests;

/// <summary>
/// Контекст videos365 поверх живой PostgreSQL: своя база на тест и схема из
/// миграций. Схема в videos365 включает <c>videos365</c>, и на провайдере в
/// памяти такая схема не проверялась вовсе.
/// </summary>
internal sealed class Videos365TestDbContextFactory
    : PostgresTestDbContextFactory<Videos365DbContext>;
