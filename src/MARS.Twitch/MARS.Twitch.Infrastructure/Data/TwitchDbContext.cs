using MARS.Platform.Infrastructure.Postgres;
using MARS.Twitch.Domain.Entities;
using MARS.Twitch.Domain.Entities.Subs;
using Microsoft.EntityFrameworkCore;

namespace MARS.Twitch.Infrastructure.Data;

public class TwitchDbContext(DbContextOptions<TwitchDbContext> options) : DbContext(options)
{
    public DbSet<TwitchUser> TwitchUsers { get; set; } = null!;
    public DbSet<TokenInfo> TwitchToken { get; set; } = null!;
    public DbSet<ChannelRewardRecord> ChannelRewards { get; set; } = null!;
    public DbSet<AutoMessage> AutoMessages { get; set; } = null!;
    public DbSet<FollowerInfo> FollowersEntitys { get; set; } = null!;
    public DbSet<TwitchLeaderboardUser> TwitchLeaderboardUsers { get; set; } = null!;
    public DbSet<HelloVideosUsers> HelloVideosUsers { get; set; } = null!;
    public DbSet<RootState> RootState { get; set; } = null!;
    public DbSet<SevenTvEmote> SevenTvEmotes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder
            .Entity<ChannelRewardRecord>()
            .HasIndex(e => e.RewardCode)
            .IsUnique()
            .HasFilter("\"RewardCode\" IS NOT NULL");

        modelBuilder
            .Entity<ChannelRewardRecord>()
            .HasIndex(e => e.TwitchRewardId)
            .IsUnique()
            .HasFilter("\"TwitchRewardId\" IS NOT NULL");

        modelBuilder
            .Entity<ChannelRewardRecord>()
            .HasIndex(e => new { e.Cost, e.NormalizedEmoji })
            .IsUnique()
            .HasFilter("\"NormalizedEmoji\" IS NOT NULL");

        modelBuilder.HasDefaultSchema("twitch");

        // FollowerInfo -> TwitchUser (one-to-one)
        modelBuilder
            .Entity<FollowerInfo>()
            .HasOne(fi => fi.TwitchUser)
            .WithOne()
            .HasForeignKey<FollowerInfo>(fi => fi.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // TwitchLeaderboardUser -> TwitchUser
        modelBuilder
            .Entity<TwitchLeaderboardUser>()
            .HasOne(tlu => tlu.TwitchUser)
            .WithMany()
            .HasForeignKey(tlu => tlu.TwitchId)
            .OnDelete(DeleteBehavior.Restrict);

        // HelloVideosUsers -> TwitchUser
        modelBuilder
            .Entity<HelloVideosUsers>()
            .HasOne(hvu => hvu.TwitchUser)
            .WithMany()
            .HasForeignKey(hvu => hvu.TwitchId)
            .OnDelete(DeleteBehavior.Restrict);
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
