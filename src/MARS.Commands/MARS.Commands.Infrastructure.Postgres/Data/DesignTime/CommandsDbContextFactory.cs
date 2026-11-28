using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.Commands.Infrastructure.Postgres.Data.DesignTime;

[ExcludeFromCodeCoverage]
public class CommandsDbContextFactory : IDesignTimeDbContextFactory<CommandsDbContext>
{
    public CommandsDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<CommandsDbContext>();
        var connectionString = configuration.GetConnectionString("CommandsDb");
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "commands")
        );

        return new CommandsDbContext(optionsBuilder.Options);
    }
}
