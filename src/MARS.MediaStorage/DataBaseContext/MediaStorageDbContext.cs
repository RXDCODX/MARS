using MARS.MediaStorage.Entities;
using MARS.Shared.Models.Media;
using Microsoft.EntityFrameworkCore;

namespace MARS.MediaStorage.DataBaseContext;

public class MediaStorageDbContext : DbContext
{
    // Только MediaEntries. Явные DbSet<MemeType>/DbSet<MemeOrder> добавлять нельзя:
    // EF берёт имя таблицы из имени свойства DbSet, поэтому такие объявления
    // переименовали бы существующие таблицы RandomMemeType/RandomMemeOrder,
    // а миграция начала бы требовать их переименования при каждом старте.
    public DbSet<MediaStorageEntry> MediaEntries => Set<MediaStorageEntry>();

    public MediaStorageDbContext(DbContextOptions<MediaStorageDbContext> options)
        : base(options) { }

    public DbSet<MediaInfo> Alerts { get; set; } = null!;
    public DbSet<MemeOrder> RandomMemeOrder { get; set; } = null!;
    public DbSet<MemeType> RandomMemeType { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("mediastorage");

        modelBuilder.Entity<MemeOrder>().HasKey(e => e.Id);
        modelBuilder
            .Entity<MemeOrder>()
            .HasOne(mo => mo.Type)
            .WithMany()
            .HasForeignKey(mo => mo.MemeTypeId);

        modelBuilder
            .Entity<MemeType>()
            .HasData([
                new MemeType
                {
                    Name = "Random Sound",
                    Id = 3,
                    // Аудит: хранилось "Alerts\\zvik" — на Linux это путь с
                    // валидным именем файла "Alerts\zvik". Seed переведён на
                    // прямой слэш, чтение дополнительно нормализует MediaPath.
                    FolderPath = "Alerts/zvik",
                },
                new MemeType
                {
                    Name = "Random Meme",
                    Id = 2,
                    FolderPath = "Alerts/random_meme",
                },
            ]);

        modelBuilder.Entity<MediaInfo>(entity =>
        {
            entity.ToTable("Alerts");

            entity.OwnsOne(
                e => e.TextInfo,
                textInfo =>
                {
                    textInfo.Property(p => p.Text).HasColumnName("TextInfo_Text");
                    textInfo.Property(p => p.TextColor).HasColumnName("TextInfo_TextColor");
                    textInfo.Property(p => p.TriggerWord).HasColumnName("TextInfo_TriggerWord");
                    textInfo.Property(p => p.KeyWordsColor).HasColumnName("TextInfo_KeyWordsColor");
                }
            );

            entity.OwnsOne(
                e => e.FileInfo,
                fileInfo =>
                {
                    fileInfo.Property(p => p.FileName).HasColumnName("FileInfo_FileName");
                    fileInfo.Property(p => p.FilePath).HasColumnName("FileInfo_LocalFilePath");
                    fileInfo.Property(p => p.Extension).HasColumnName("FileInfo_Extension");
                    fileInfo.Property(p => p.IsLocalFile).HasColumnName("FileInfo_IsLocal");
                    fileInfo
                        .Property(p => p.IsFileNotConvertable)
                        .HasColumnName("FileInfo_IsFileNotConvertable");

                    fileInfo
                        .Property(p => p.Type)
                        .HasColumnName("FileInfo_Type")
                        .HasConversion<string>();
                }
            );

            entity.OwnsOne(
                e => e.PositionInfo,
                positionInfo =>
                {
                    positionInfo.Property(p => p.Height).HasColumnName("PositionInfo_Height");
                    positionInfo.Property(p => p.Width).HasColumnName("PositionInfo_Width");
                    positionInfo.Property(p => p.Rotation).HasColumnName("PositionInfo_Rotation");
                    positionInfo
                        .Property(p => p.RandomCoordinates)
                        .HasColumnName("PositionInfo_RandomCoordinates");
                    positionInfo
                        .Property(p => p.IsProportion)
                        .HasColumnName("PositionInfo_IsProportion");
                    positionInfo.Property(p => p.IsRotated).HasColumnName("PositionInfo_IsRotated");
                    positionInfo
                        .Property(p => p.XCoordinate)
                        .HasColumnName("PositionInfo_XCoordinate");
                    positionInfo
                        .Property(p => p.YCoordinate)
                        .HasColumnName("PositionInfo_YCoordinate");
                    positionInfo
                        .Property(p => p.IsResizeRequires)
                        .HasColumnName("PositionInfo_IsResizeRequires");
                    positionInfo
                        .Property(p => p.IsHorizontalCenter)
                        .HasColumnName("PositionInfo_IsHorizontalCenter");
                    positionInfo
                        .Property(p => p.IsVerticallCenter)
                        .HasColumnName("PositionInfo_IsVerticallCenter");
                }
            );

            entity.OwnsOne(
                e => e.MetaInfo,
                metaInfo =>
                {
                    metaInfo.Property(p => p.DisplayName).HasColumnName("MetaInfo_DisplayName");
                    metaInfo.Property(p => p.IsLooped).HasColumnName("MetaInfo_IsLooped");
                    metaInfo.Property(p => p.Duration).HasColumnName("MetaInfo_Duration");
                    metaInfo
                        .Property(p => p.TwitchPointsCost)
                        .HasColumnName("MetaInfo_TwitchPointsCost");
                    metaInfo.Property(p => p.Vip).HasColumnName("MetaInfo_VIP");
                    metaInfo.Property(e => e.Priority).HasColumnName("MetaInfo_Priority");
                }
            );

            entity.OwnsOne(
                e => e.StylesInfo,
                metaInfo =>
                {
                    metaInfo.Property(p => p.IsBorder).HasColumnName("StylesInfo_IsBorder");
                    metaInfo
                        .Property(p => p.IsShowLetterbox)
                        .HasColumnName("StylesInfo_IsShowLetterbox");
                }
            );
        });
        modelBuilder.Entity<MediaStorageEntry>(entity =>
        {
            entity.ToTable("MediaEntries");

            entity.HasKey(e => e.Id);

            // Путь — естественный ключ индексации: по нему идёт обход
            // хранилища и он же служит ключом upsert при сканировании.
            entity.HasIndex(e => e.Path).IsUnique();
            entity.HasIndex(e => e.DeletedAt);

            entity.Property(e => e.Path).IsRequired();
            entity.Property(e => e.FileName).IsRequired();
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        configurationBuilder.Properties<byte[]>().HaveColumnType("bytea");
    }
}
