using MARS.CinemaQueue.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.CinemaQueue.Tests;

/// <summary>
/// Фабрика контекстов поверх готовых опций. Нужна репозиторию и сервису:
/// они создают контекст на каждый вызов, а не держат один на всё время.
/// </summary>
public sealed class TestDbContextFactory(DbContextOptions<CinemaDbContext> options)
    : IDbContextFactory<CinemaDbContext>
{
    public CinemaDbContext CreateDbContext() => new(options);
}
