using MARS.TestKit.Postgres;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.Tests;

/// <summary>
/// Контекст хранилища поверх PostgreSQL: своя база на тест и схема из миграций.
/// </summary>
public sealed class TestDbContextFactory
    : PostgresTestDbContextFactory<MARS.MediaStorage.Infrastructure.Postgres.Data.MediaStorageDbContext>;
