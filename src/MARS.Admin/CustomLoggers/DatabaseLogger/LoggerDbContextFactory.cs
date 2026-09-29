using MARS.Admin.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace MARS.Admin.CustomLoggers.DatabaseLogger;

public class LoggerDbContextFactory
    : IDbContextFactory<LoggerDbContext>,
        IDesignTimeDbContextFactory<LoggerDbContext>
{
    private readonly DbContextOptions<LoggerDbContext>? _options;

    public LoggerDbContextFactory(Action<DbContextOptionsBuilder<LoggerDbContext>> optionsAction)
    {
        var optionsBuilder = new DbContextOptionsBuilder<LoggerDbContext>();
        optionsAction.Invoke(optionsBuilder);
        _options = optionsBuilder.Options;
    }

    public LoggerDbContextFactory()
    {
    }

    public LoggerDbContext CreateDbContext()
    {
        return GetDbContext(false);
    }

    public LoggerDbContext CreateDbContext(string[] args)
    {
        return GetDbContext(true);
    }

    private LoggerDbContext GetDbContext(bool isMigrations)
    {
        if (_options != null && !isMigrations)
        {
            return new LoggerDbContext(_options, isMigrations);
        }

        var optionsBuilder = new DbContextOptionsBuilder<LoggerDbContext>();

        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile($"appsettings.Development.json", optional: true)
            .Build();

        var connectionString = configuration.GetConnectionString("LogsDb");

        optionsBuilder.UseNpgsql(connectionString);
        optionsBuilder.UseQueryTrackingBehavior(QueryTrackingBehavior.TrackAll);
        optionsBuilder.EnableThreadSafetyChecks();

        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
        if (environment == Environments.Development)
        {
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging();
        }

        return new LoggerDbContext(optionsBuilder.Options, true);
    }
}
