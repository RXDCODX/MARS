using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;

namespace MARS.Shared.Extensions;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// HTTP клиент к другому сервису с tracing и retry (Polly v8)
    /// </summary>
    public static IServiceCollection AddMarsHttpClient<TClient>(
        this IServiceCollection services,
        string serviceName,
        string baseAddress
    )
        where TClient : class
    {
        services
            .AddHttpClient<TClient>(client =>
            {
                client.BaseAddress = new Uri(baseAddress);
                client.DefaultRequestHeaders.UserAgent.Add(
                    new ProductInfoHeaderValue("MARS", serviceName)
                );
            })
            .AddStandardResilienceHandler();

        return services;
    }

    /// <summary>
    /// DbContext с автоматическими миграциями
    /// </summary>
    public static IServiceCollection AddMarsDbContext<TContext>(
        this IServiceCollection services,
        IConfiguration configuration,
        string schema,
        bool applyMigrations = true
    )
        where TContext : DbContext
    {
        var connString = MarsConnectionStringResolver.Resolve(configuration);

        services.AddDbContextFactory<TContext>(options =>
        {
            options.UseNpgsql(
                connString,
                npgsql => npgsql.MigrationsHistoryTable("__EFMigrationsHistory", schema)
            );
        });

        if (applyMigrations)
        {
            services.AddMarsSchemaMigration<TContext>();
        }

        return services;
    }

    /// <summary>
    /// Применяет pending-миграции при старте хоста с ретраями.
    /// Миграции выполняются синхронно до <c>app.Run()</c> — см.
    /// <see cref="MarsSchemaStartupExtensions.RunMarsSchemaMigrationsAsync"/>,
    /// иначе фоновые сервисы успевали обратиться к несуществующим таблицам.
    /// </summary>
    public static IServiceCollection AddMarsSchemaMigration<TContext>(
        this IServiceCollection services
    )
        where TContext : DbContext
    {
        services.AddSingleton<MarsSchemaMigrator<TContext>>();

        services.AddSingleton<IMarsSchemaReady<TContext>>(
            sp => new MarsSchemaReadySignal<TContext>(sp.GetRequiredService<MarsSchemaMigrator<TContext>>())
        );

        services.AddSingleton(
            sp => new MarsSchemaStartupMarker(
                typeof(TContext).Name,
                ct => sp.GetRequiredService<MarsSchemaMigrator<TContext>>().MigrateAsync(ct)
            )
        );

        services.AddHostedService<MarsSchemaMigrationHostedService<TContext>>();

        return services;
    }
}
