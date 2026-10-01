using MARS.Alerts.Data;
using MARS.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.Alerts.Data.DesignTime;

/// <summary>
/// Фабрика для <c>dotnet ef</c>. Строка подключения та же, что и в рантайме,
/// иначе миграции применялись бы к одной базе, а сервис работал с другой.
/// </summary>
public class AlertsDbContextFactory : IDesignTimeDbContextFactory<AlertsDbContext>
{
    public AlertsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AlertsDbContext>();
        var connectionString = MarsConnectionStringResolver.Resolve(configuration, "AlertsDb");

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "alerts")
        );

        return new AlertsDbContext(optionsBuilder.Options);
    }
}
