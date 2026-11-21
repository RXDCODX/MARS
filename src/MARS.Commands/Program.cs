using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Platform.Infrastructure.Clients.Http;
using MARS.Platform.Infrastructure.Hosting.Extensions;
using MARS.Platform.Infrastructure.Messaging.RabbitMq;
using MARS.Platform.Infrastructure.Persistence.Postgres.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace MARS.Commands;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Commands");
        builder.AddMarsWolverine("MARS.Commands", typeof(Program).Assembly);

        builder.Services.AddHttpClient();
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

        // Межсервисные HTTP-клиенты: команды обращаются к данным, чьими
        // владельцами являются другие сервисы (таблица лидеров — MARS.TwitchCore).
        builder.Services.AddMarsServiceClients(builder.Configuration);
        builder.Services.AddMarsServiceClient<ILeaderboardClient, LeaderboardClient>(
            ServiceClientExtensions.LeaderboardHttpClientName
        );
        builder.Services.AddMarsServiceClient<IWaifuGachaClient, WaifuGachaClient>(
            ServiceClientExtensions.WaifuGachaHttpClientName
        );

        // Справочник пользователей Twitch: команды, принимающие логин, хранят
        // данные по Twitch ID.
        builder.Services.AddMarsServiceClient<ITwitchUserClient, TwitchUserClient>(
            ServiceClientExtensions.LeaderboardHttpClientName
        );

        // REST-outbox: ответы команд уходят в сервисы платформ через их internal-эндпоинты.
        builder.Services.AddMarsTwitchChatClient();
        builder.Services.AddMarsDiscordMessageClient();
        builder.Services.AddMarsTelegramMessageClient();
        builder.Services.AddMarsAlertsCommandClient();

        // Клиент к MARS.AudioController: команды mutesound/unmutesound.
        builder.Services.AddMarsAudioControllerClient();

        var app = builder.Build();

        app.UseMarsDefaults();
        app.MapControllers();
        app.MapGet("/", () => "MARS.Commands is running");

        // Миграции применяются ДО старта хоста: иначе фоновые сервисы успевают
        // обратиться к ещё не созданным таблицам (42P01).
        app.RunMarsSchemaMigrationsAsync().GetAwaiter().GetResult();

        app.Run();
    }
}
