using MARS.Scoreboard.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.Scoreboard.Tests.Grpc;

public sealed class TestDbContextFactory(DbContextOptions<ScoreboardDbContext> options)
    : IDbContextFactory<ScoreboardDbContext>
{
    public ScoreboardDbContext CreateDbContext() => new(options);
}
