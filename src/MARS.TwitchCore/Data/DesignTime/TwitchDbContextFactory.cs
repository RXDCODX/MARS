using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.TwitchCore.Data.DesignTime;

public class TwitchDbContextFactory : IDesignTimeDbContextFactory<TwitchDbContext>
{
    public TwitchDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<TwitchDbContext>();
        var connectionString = configuration.GetConnectionString("TwitchDb");
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "twitch")
        );

        return new TwitchDbContext(optionsBuilder.Options);
    }
}
