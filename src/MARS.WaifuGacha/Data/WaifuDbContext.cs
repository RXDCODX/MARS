using MARS.WaifuGacha.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MARS.WaifuGacha.Data;

public class WaifuDbContext : DbContext
{
    public WaifuDbContext(DbContextOptions<WaifuDbContext> options)
        : base(options) { }

    public DbSet<Waifu> Waifus { get; set; } = null!;
    public DbSet<Husband> Husbands { get; set; } = null!;
    public DbSet<HusbandCoolDown> HusbandCoolDowns { get; set; } = null!;
    public DbSet<HusbandAutoHello> HusbandGreetings { get; set; } = null!;
    public DbSet<WaifuRollGuarantee> WaifuRollGuarantees { get; set; } = null!;
    public DbSet<WaifuRollAudio> WaifuRollAudios { get; set; } = null!;
    public DbSet<AutoHelloMessage> AutoHelloMessages { get; set; } = null!;
    public DbSet<Fumo> Fumos { get; set; } = null!;
    public DbSet<Frog> Frogs { get; set; } = null!;
    public DbSet<MikuModule> MikuModules { get; set; } = null!;
    public DbSet<UserFumoCollection> UserFumoCollections { get; set; } = null!;
    public DbSet<UserMikuCollection> UserMikuCollections { get; set; } = null!;
    public DbSet<MikuMondayTrack> MikuMondayTracks { get; set; } = null!;
    public DbSet<MikuMondayActivation> MikuMondayActivations { get; set; } = null!;
    public DbSet<RollCooldown> RollCooldowns { get; set; } = null!;
    public DbSet<RootState> RootState { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("waifu");

        modelBuilder.Entity<AutoHelloMessage>().HasData(AutoHelloMessageSeed.Build());
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
