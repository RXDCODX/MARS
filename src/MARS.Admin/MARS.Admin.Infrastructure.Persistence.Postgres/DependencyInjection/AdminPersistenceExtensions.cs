using MARS.Admin.Application.Services.ServiceManager;
using MARS.Admin.Infrastructure.Persistence.Postgres.Configuration;
using MARS.Admin.Infrastructure.Persistence.Postgres.Data;
using MARS.Admin.Infrastructure.Persistence.Postgres.Repositories;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Admin.Infrastructure.Persistence.Postgres.DependencyInjection;

public static class AdminPersistenceExtensions
{
    public static IServiceCollection AddAdminPersistence(
        this IServiceCollection services,
        string connectionString
    )
    {
        services.AddDbContextFactory<AdminDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "admin")
            );
            options.UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking);
        });

        services.AddMarsSchemaMigration<AdminDbContext>();
        services.AddSingleton<IServiceStateRepository, AdminServiceStateRepository>();
        services.AddHostedService<ConfigurationKeysBootstrapHostedService>();

        return services;
    }
}
