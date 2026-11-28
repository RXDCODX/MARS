using MARS.Commands.Domain.Entities;
using MARS.Commands.Domain.Enums;
using MARS.Platform.Infrastructure.Postgres;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Infrastructure.Postgres.Data;

[ExcludeFromCodeCoverage]
public class CommandsDbContext : DbContext
{
    public CommandsDbContext(DbContextOptions<CommandsDbContext> options)
        : base(options) { }

    public DbSet<CommandExecution> CommandExecutions { get; set; } = null!;

    public DbSet<CommandOutboxMessage> CommandOutboxMessages { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("commands");

        modelBuilder.Entity<CommandExecution>(entity =>
        {
            entity.ToTable("CommandExecutions");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.RequestId).IsRequired().HasMaxLength(256);
            entity.HasIndex(x => x.RequestId).IsUnique();
            entity.Property(x => x.Platform).IsRequired().HasMaxLength(64);
            entity.Property(x => x.CommandName).IsRequired().HasMaxLength(128);
            entity.Property(x => x.CallerId).IsRequired().HasMaxLength(256);
            entity.Property(x => x.CallerDisplayName).HasMaxLength(256);
            entity.Property(x => x.InputHash).IsRequired().HasMaxLength(128);
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.ErrorCode).HasMaxLength(128);
            entity.Property(x => x.FailureReason).HasMaxLength(2048);
            entity.Property(x => x.JobId).HasMaxLength(128);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.Status, x.AcceptedAtUtc });
        });

        modelBuilder.Entity<CommandOutboxMessage>(entity =>
        {
            entity.ToTable("CommandOutboxMessages");
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Payload).IsRequired();
            entity.Property(x => x.Status).IsRequired();
            entity.Property(x => x.Error).HasMaxLength(2048);
            entity.Property(x => x.RowVersion).IsRowVersion();
            entity.HasIndex(x => new { x.ExecutionId, x.MessageType }).IsUnique();
            entity.HasIndex(x => new { x.Status, x.NextAttemptAtUtc });
            entity.HasOne<CommandExecution>()
                .WithMany()
                .HasForeignKey(x => x.ExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);
        configurationBuilder.ConfigureUtcDates();
    }
}