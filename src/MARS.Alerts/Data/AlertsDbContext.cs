using MARS.Alerts.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.Alerts.Data;

public sealed class AlertsDbContext(DbContextOptions<AlertsDbContext> options) : DbContext(options)
{
    /// <summary>
    /// Настройка раскладки ADHD-экрана. Конвейера публикации видео здесь нет:
    /// он живёт в MARS.Videos365 вместе с собственной базой.
    /// </summary>
    public DbSet<AdhdLayoutConfig> AdhdLayoutConfig => Set<AdhdLayoutConfig>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.HasDefaultSchema("alerts");

        builder.Entity<AdhdLayoutConfig>(entity =>
        {
            entity.ToTable("AdhdLayoutConfig");

            // Настройка одна на весь сервис, поэтому ключ задаётся явно, а не
            // генерируется: иначе появилась бы вторая строка с Id = 1 и
            // расхождением по составу виджетов.
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("now()");
        });
    }
}
