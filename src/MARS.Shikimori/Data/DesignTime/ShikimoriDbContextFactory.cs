using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.Shikimori.Data.DesignTime;

public class ShikimoriDbContextFactory : IDesignTimeDbContextFactory<ShikimoriDbContext>
{
    public ShikimoriDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ShikimoriDbContext>();
        var connectionString = configuration.GetConnectionString("ShikimoriDb");
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "shikimori")
        );

        return new ShikimoriDbContext(optionsBuilder.Options);
    }
}
