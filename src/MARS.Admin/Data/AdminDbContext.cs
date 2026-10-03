using MARS.Admin.Entities;
using MARS.Shared.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.Admin.Data;

public class AdminDbContext : DbContext
{
    public AdminDbContext(DbContextOptions<AdminDbContext> options)
        : base(options) { }

    public DbSet<ServiceState> ServiceStates { get; set; } = null!;
    public DbSet<EnvironmentVariable> EnvironmentVariables { get; set; } = null!;
    public DbSet<RootState> RootState { get; set; } = null!;
    public DbSet<SevenTvEmote> SevenTvEmotes { get; set; } = null!;
    public DbSet<FollowerInfo> FollowerInfos { get; set; } = null!;
    public DbSet<TwitchUser> TwitchUsers { get; set; } = null!;
    public DbSet<StreamArchiveConfig> StreamArchiveConfigs { get; set; } = null!;
    public DbSet<StreamArchiveFile> StreamArchiveFiles { get; set; } = null!;
    public DbSet<StreamArchiveFileChunk> StreamArchiveFileChunks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("admin");

        builder.Entity<EnvironmentVariable>().Property(e => e.Key).HasMaxLength(500);
        builder.Entity<RootState>().Property(e => e.Name).HasMaxLength(500);

        builder
            .Entity<StreamArchiveFile>()
            .HasOne(f => f.Config)
            .WithMany()
            .HasForeignKey(f => f.ConfigId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<StreamArchiveFileChunk>()
            .HasOne(c => c.File)
            .WithMany(f => f.Chunks)
            .HasForeignKey(c => c.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .Entity<FollowerInfo>()
            .HasOne(f => f.TwitchUser)
            .WithMany()
            .HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetConversion>();

        configurationBuilder.ConfigureUtcDates();
    }

    public sealed class DateTimeOffsetConversion()
        : ValueConverter<DateTimeOffset, DateTimeOffset>(
            offset => offset.Offset != TimeSpan.Zero ? offset.ToOffset(TimeSpan.Zero) : offset,
            v => v.ToLocalTime()
        );
}
