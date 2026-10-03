using MARS.Shared.Data;
using MARS.Shikimori.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.Shikimori.Data;

/// <summary>
/// База <c>MARS.Shikimori</c> — собственная строка подключения и своя схема.
/// </summary>
public class ShikimoriDbContext : DbContext
{
    public ShikimoriDbContext(DbContextOptions<ShikimoriDbContext> options)
        : base(options) { }

    public DbSet<ShikimoriCharacter> Characters { get; set; } = null!;

    public DbSet<ShikimoriTitlePick> TitlePicks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("shikimori");

        modelBuilder.Entity<ShikimoriTitlePick>().Property(pick => pick.Id).ValueGeneratedOnAdd();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureUtcDates();
    }
}
