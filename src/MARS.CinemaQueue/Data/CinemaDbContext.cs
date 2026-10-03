using MARS.CinemaQueue.Entities;
using MARS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.CinemaQueue.Data;

public class CinemaDbContext : DbContext
{
    public CinemaDbContext(DbContextOptions<CinemaDbContext> options)
        : base(options) { }

    public DbSet<CinemaMediaItem> CinemaQueue { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("cinema");

        modelBuilder.Entity<CinemaMediaItem>().Property(e => e.Id).ValueGeneratedOnAdd();
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureUtcDates();
    }
}
