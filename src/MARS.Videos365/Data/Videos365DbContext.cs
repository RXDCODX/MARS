using MARS.Shared.Data;
using MARS.Videos365.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Videos365.Data;

public sealed class Videos365DbContext(DbContextOptions<Videos365DbContext> options)
    : DbContext(options)
{
    public DbSet<Video365> Videos365 => Set<Video365>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("videos365");

        builder.Entity<Video365>(entity =>
        {
            entity.ToTable("Videos365");

            entity.HasKey(e => e.Id);

            // Дедупликация идёт по SiteId на каждом проходе конвейера, а в
            // монолите индекс на него не было: каждая проверка «выложено ли
            // уже» делала последовательное чтение таблицы. Уникальность
            // дополнительно закрывает повторную вставку того же видео.
            entity.HasIndex(e => e.SiteId).IsUnique();
        });
    }

    /// <summary>
    /// Приведение дат к UTC — общее правило репозитория: Npgsql отвергает
    /// <c>DateTime</c> с <c>Kind=Local</c>, а в коде сервисов полно
    /// <c>DateTime.Now</c>. Без этого любая запись в колонку
    /// <c>timestamp with time zone</c> падала бы на живом PostgreSQL.
    /// </summary>
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureUtcDates();
    }
}
