using MARS.Commands.Application.Services;
using MARS.Commands.Application.Services.Adapters;
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

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Commands");
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
        builder.Services.AddHostedService(sp =>
            (CommandExecutorService)sp.GetRequiredService<ICommandService>()
        );

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
        app.MapGet("/", () => "MARS.Commands is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
