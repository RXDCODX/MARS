using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.Scoreboard.Data.DesignTime;

/// <summary>
/// Фабрика только для инструментов EF Core (<c>dotnet ef</c>).
/// <para>
/// Схема <c>scoreboard</c> объявляется в коде, а не в конфигурации, поэтому
/// собранному приложению достаточно своей строки подключения; инструментам,
/// которые не запускают <c>Program</c>, нужен отдельный вход.
/// </para>
/// </summary>
public class ScoreboardDbContextFactory : IDesignTimeDbContextFactory<ScoreboardDbContext>
{
    public ScoreboardDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ScoreboardDbContext>();
        var connectionString = configuration.GetConnectionString("ScoreboardDb");
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "scoreboard")
        );

        return new ScoreboardDbContext(optionsBuilder.Options);
    }
}
