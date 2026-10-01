using MARS.Telegram.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.Telegram.Data;

public class ChatDbContext : DbContext
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options)
        : base(options) { }

    public DbSet<TelegramUser> TelegramUsers { get; set; } = null!;
    public DbSet<WTelegramSession> WTelegramSessions { get; set; } = null!;
    public DbSet<TelegramUpdateReceiverOffset> TelegramUpdateReceiverOffsets { get; set; } = null!;
    public DbSet<TelegramDiscordChannelBinding> TelegramDiscordChannelBindings { get; set; } = null!;
    public DbSet<TelegramDiscordChannelState> TelegramDiscordChannelStates { get; set; } = null!;
    public DbSet<ChannelProcessingState> ChannelProcessingStates { get; set; } = null!;
    public DbSet<RootState> RootState { get; set; } = null!;

    /// <summary>
    /// Правила автоматической публикации изображений и их расписание.
    /// Перенесены из монолита вместе с данными; кода автопостинга в новом
    /// репозитории пока нет (см. <see cref="BooruAutoPostConfig"/>).
    /// </summary>
    public DbSet<BooruAutoPostConfig> BooruAutoPostConfigs { get; set; } = null!;
    public DbSet<BooruScheduledPost> BooruScheduledPosts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("chat");

        modelBuilder
            .Entity<TelegramDiscordChannelBinding>()
            .HasIndex(e => new { e.TelegramChannelId, e.DiscordChannelId })
            .IsUnique();

        modelBuilder
            .Entity<TelegramDiscordChannelBinding>()
            .Property(e => e.DiscordChannelId)
            .HasConversion(new NumberToStringConverter<ulong>());

        modelBuilder.Entity<TelegramDiscordChannelState>().HasKey(e => e.TelegramChannelId);

        modelBuilder
            .Entity<BooruAutoPostConfig>(entity =>
            {
                entity.ToTable("BooruAutoPostConfigs");

                // В монолите тип varchar(64). Ограничение отдаётся базе, а не
                // только аннотации: иначе слишком длинный идентификатор канала
                // молча обрезался бы уже в хранилище.
                entity.Property(e => e.DiscordChannelId).HasMaxLength(64);
            }
        );

        modelBuilder
            .Entity<BooruScheduledPost>(entity =>
            {
                entity.ToTable("BooruScheduledPosts");

                entity.HasIndex(e => e.ConfigId);

                entity
                    .HasOne(e => e.Config)
                    .WithMany()
                    .HasForeignKey(e => e.ConfigId)
                    .OnDelete(DeleteBehavior.Cascade);
            }
        );
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder
            .Properties<DateTimeOffset>()
            .HaveConversion<DateTimeOffsetConversion>();

        configurationBuilder.Properties<DateTime>().HaveConversion<DateTimeToDateTimeUtc>();
    }

    public sealed class DateTimeOffsetConversion()
        : ValueConverter<DateTimeOffset, DateTimeOffset>(
            offset => offset.Offset != TimeSpan.Zero ? offset.ToOffset(TimeSpan.Zero) : offset,
            v => v.ToLocalTime()
        );

    public sealed class DateTimeToDateTimeUtc()
        : ValueConverter<DateTime, DateTime>(
            c => DateTime.SpecifyKind(c, DateTimeKind.Utc),
            c => c
        );
}
