using MARS.MediaStorage.DataBaseContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace MARS.MediaStorage.DataBaseContext.DesignTime;

public class MediaStorageDbContextFactory : IDesignTimeDbContextFactory<MediaStorageDbContext>
{
    public MediaStorageDbContext CreateDbContext(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
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
