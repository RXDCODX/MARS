using MARS.TwitchCore.Entities;
using MARS.TwitchCore.Entities.Subs;
using Microsoft.EntityFrameworkCore;

namespace MARS.TwitchCore.Data;

public class TwitchDbContext : DbContext
{
    public TwitchDbContext(DbContextOptions<TwitchDbContext> options)
        : base(options) { }

    public DbSet<TwitchUser> TwitchUsers { get; set; } = null!;
    public DbSet<TokenInfo> TwitchToken { get; set; } = null!;
    public DbSet<ChannelRewardRecord> ChannelRewards { get; set; } = null!;
    public DbSet<AutoMessage> AutoMessages { get; set; } = null!;
    public DbSet<FollowerInfo> FollowersEntitys { get; set; } = null!;
    public DbSet<FumoUser> FumoUsers { get; set; } = null!;
    public DbSet<TwitchLeaderboardUser> TwitchLeaderboardUsers { get; set; } = null!;
    public DbSet<HelloVideosUsers> HelloVideosUsers { get; set; } = null!;
    public DbSet<RollCooldown> RollCooldowns { get; set; } = null!;
    public DbSet<RootState> RootState { get; set; } = null!;
    public DbSet<Husband> Husbands { get; set; } = null!;
    public DbSet<SevenTvEmote> SevenTvEmotes { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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

        // FumoUser -> TwitchUser
        modelBuilder
            .Entity<FumoUser>()
            .HasOne(fu => fu.TwitchUser)
            .WithMany()
            .HasForeignKey(fu => fu.TwitchId)
            .OnDelete(DeleteBehavior.Restrict);

        // RollCooldowns: unique index on (TwitchUserId, RollType)
        modelBuilder
            .Entity<RollCooldown>()
            .HasIndex(e => new { e.TwitchUserId, e.RollType })
            .IsUnique();

        // Husband -> TwitchUser
        modelBuilder
            .Entity<Husband>()
            .HasOne(h => h.TwitchUser)
            .WithOne()
            .HasForeignKey<Husband>(h => h.TwitchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
