using MARS.Scoreboard.Entities;
using MARS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.Scoreboard.Data;

public class ScoreboardDbContext : DbContext
{
    public ScoreboardDbContext(DbContextOptions<ScoreboardDbContext> options)
        : base(options) { }

    public DbSet<ScoreboardState> ScoreboardStates { get; set; } = null!;
    public DbSet<ScoreboardPlayer> ScoreboardPlayers { get; set; } = null!;
    public DbSet<ScoreboardLayout> ScoreboardLayouts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("scoreboard");

        modelBuilder
            .Entity<ScoreboardState>()
            .HasMany(s => s.Players)
            .WithOne(p => p.ScoreboardState)
            .HasForeignKey(p => p.ScoreboardStateId);

        modelBuilder
            .Entity<ScoreboardState>()
            .HasOne(s => s.Layout)
            .WithOne(l => l.ScoreboardState)
            .HasForeignKey<ScoreboardLayout>(l => l.ScoreboardStateId);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureUtcDates();
    }
}
