using System.Diagnostics.CodeAnalysis;
using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Adapters;
using MARS.Commands.Host.Services;
using MARS.Commands.Infrastructure.Postgres;
using MARS.Commands.Infrastructure.Postgres.Data;
using MARS.Commands.Infrastructure.Services.Adapters;
using MARS.Commands.Infrastructure.Services.Messaging;
using MARS.Platform.Infrastructure.Grpc.Extensions;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Http;
using MARS.Platform.Infrastructure.Postgres.Extensions;
using MARS.Platform.Infrastructure.RabbitMq;
using MARS.Platform.Infrastructure.Web.AspNetCore.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Commands.Host;

[ExcludeFromCodeCoverage]
public class Program
{
    private static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Commands", "CommandsDb");

        // Commands-owned PostgreSQL ledger/outbox. Migrations run before app.Run().
        builder.Services.AddMarsDbContext<CommandsDbContext>(
            builder.Configuration,
            "commands",
            "CommandsDb"
        );
        builder.Services.AddCommandsPersistence();

        builder.AddMarsGrpcHosting();
        builder.Services.AddMarsGrpcClient();
        builder.Services.AddMarsAudioControllerClient(builder.Configuration);
        builder.AddMarsWolverine(
            "MARS.Commands",
            typeof(Program).Assembly,
            typeof(CommandRequestListener).Assembly
        );

        builder.Services.AddControllers();

        // Command executor services
        builder.Services.AddSingleton<CommandFactory>();
        builder.Services.AddSingleton<ICommandService, CommandExecutorService>();
        builder.Services.AddScoped<ICommandExecutionLedger, CommandExecutionLedger>();
        builder.Services.AddScoped<ICommandResponseSender, GrpcCommandResponseSender>();
        builder.Services.AddHostedService(sp =>
            (CommandExecutorService)sp.GetRequiredService<ICommandService>()
        );
        builder.Services.AddHostedService<CommandOutboxDispatcher>();
        builder.Services.AddHostedService<MediaDownloadReconciliationWorker>();
        builder.Services.AddSingleton<MediaDownloadJobFinalizer>();

        // Platform adapters
        builder.Services.AddSingleton<ApiCommandService>();
        builder.Services.AddSingleton<TwitchCommandService>();
        builder.Services.AddSingleton<DiscordCommandService>();
        builder.Services.AddSingleton<TelegramCommandService>();

        // Outbox-ответы команд доставляются в Twitch, Discord и Telegram по gRPC.

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();

        app.MapGrpcService<MARS.Commands.Api.Grpc.CommandsControlGrpcService>()
            .RequireAuthorization(ServiceAuthExtensions.PolicyName);
        app.MapGrpcService<MARS.Commands.Api.Grpc.CommandsCatalogGrpcService>()
            .RequireAuthorization(ServiceAuthExtensions.PolicyName);
        app.MapGet("/", () => "MARS.Commands is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        await app.RunMarsSchemaMigrationsAsync();

        await app.RunAsync();
    }
}
