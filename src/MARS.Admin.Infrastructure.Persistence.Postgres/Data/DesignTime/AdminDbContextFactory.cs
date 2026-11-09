using MARS.Admin.Infrastructure.Persistence.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.Admin.Infrastructure.Persistence.Postgres.Data.DesignTime;

public class AdminDbContextFactory : IDesignTimeDbContextFactory<AdminDbContext>
{
    public AdminDbContext CreateDbContext(string[] args)
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var apiDirectoryCandidates = new[]
        {
            Path.Combine(currentDirectory, "src", "MARS.Admin.Api"),
            Path.Combine(currentDirectory, "..", "MARS.Admin.Api"),
            Path.Combine(currentDirectory, "MARS.Admin.Api"),
        };
        var configurationDirectory =
            apiDirectoryCandidates.FirstOrDefault(Directory.Exists) ?? currentDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<AdminDbContext>();
        var connectionString = configuration.GetConnectionString("AdminDb");
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "admin")
        );

        return new AdminDbContext(optionsBuilder.Options);
    }
}
