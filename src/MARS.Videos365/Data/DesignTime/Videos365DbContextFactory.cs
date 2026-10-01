using MARS.Shared.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.Videos365.Data.DesignTime;

/// <summary>
/// Фабрика для <c>dotnet ef</c>. Строка подключения та же, что и в рантайме,
/// иначе миграции применялись бы к одной базе, а сервис работал с другой.
/// </summary>
public class Videos365DbContextFactory : IDesignTimeDbContextFactory<Videos365DbContext>
{
    public Videos365DbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<Videos365DbContext>();
        var connectionString = MarsConnectionStringResolver.Resolve(configuration, "Videos365Db");

        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "videos365")
        );

        return new Videos365DbContext(optionsBuilder.Options);
    }
}
