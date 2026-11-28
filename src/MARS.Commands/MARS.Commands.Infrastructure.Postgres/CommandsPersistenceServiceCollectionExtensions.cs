using MARS.Commands.Application.Services;
using MARS.Commands.Infrastructure.Postgres.Data;
using MARS.Commands.Infrastructure.Postgres.Repositories;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;

namespace MARS.Commands.Infrastructure.Postgres;

public static class CommandsPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddCommandsPersistence(this IServiceCollection services)
    {
        services.AddScoped<ICommandExecutionRepository, CommandExecutionRepository>();
        services.AddScoped<ICommandOutboxRepository, CommandOutboxRepository>();
        return services;
    }
}