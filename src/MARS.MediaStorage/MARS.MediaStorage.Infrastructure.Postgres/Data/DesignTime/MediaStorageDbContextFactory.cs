using MARS.MediaStorage.Infrastructure.Postgres.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.MediaStorage.Infrastructure.Postgres.Data.DesignTime;

public class MediaStorageDbContextFactory : IDesignTimeDbContextFactory<MediaStorageDbContext>
{
    public MediaStorageDbContext CreateDbContext(string[] args)
    {
        // Настройки живут в Host: dotnet ef можно запускать и из корня
        // репозитория, и из каталога слоя, поэтому ищем хост относительно
        // текущего каталога, как это делает AdminDbContextFactory.
        var currentDirectory = Directory.GetCurrentDirectory();
        var hostDirectoryCandidates = new[]
        {
            Path.Combine(currentDirectory, "src", "MARS.MediaStorage", "MARS.MediaStorage.Host"),
            Path.Combine(currentDirectory, "..", "MARS.MediaStorage.Host"),
            Path.Combine(currentDirectory, "MARS.MediaStorage.Host"),
        };
        var configurationDirectory =
            hostDirectoryCandidates.FirstOrDefault(Directory.Exists) ?? currentDirectory;

        var configuration = new ConfigurationBuilder()
            .SetBasePath(configurationDirectory)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<MediaStorageDbContext>();
        var connectionString = configuration.GetConnectionString("MediaStorageDb");
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "mediastorage")
        );

        return new MediaStorageDbContext(optionsBuilder.Options);
    }
}
