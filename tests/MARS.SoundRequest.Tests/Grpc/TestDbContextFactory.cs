using MARS.SoundRequest.Data;
using Microsoft.EntityFrameworkCore;

namespace MARS.SoundRequest.Tests.Grpc;

public sealed class TestDbContextFactory(DbContextOptions<MediaDbContext> options)
    : IDbContextFactory<MediaDbContext>
{
    public MediaDbContext CreateDbContext() => new(options);
}
