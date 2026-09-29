using MARS.SoundRequest.Entities;
using Microsoft.EntityFrameworkCore;

namespace MARS.SoundRequest.Data;

public class MediaDbContext(DbContextOptions<MediaDbContext> options) : DbContext(options)
{
    public DbSet<BaseTrackInfo> Tracks => Set<BaseTrackInfo>();
    public DbSet<QueueItem> QueueItems => Set<QueueItem>();
    public DbSet<PlayerState> PlayerStates => Set<PlayerState>();
    public DbSet<RootState> RootState => Set<RootState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("media");

        modelBuilder.Entity<BaseTrackInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TrackName).HasMaxLength(300);
            entity.Property(e => e.Url).IsRequired();
            entity.HasQueryFilter(e => !e.IsDeleted);
        });

        modelBuilder.Entity<QueueItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Track)
                .WithMany()
                .HasForeignKey(e => e.TrackId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.Property(e => e.RequestedByTwitchId).HasMaxLength(50);
        });

        modelBuilder.Entity<PlayerState>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Ignore(e => e.StateVersion);
            entity.HasOne(e => e.CurrentQueueItem)
                .WithMany()
                .HasForeignKey(e => e.CurrentQueueItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<RootState>(entity =>
        {
            entity.HasKey(e => e.Name);
        });
    }
}
