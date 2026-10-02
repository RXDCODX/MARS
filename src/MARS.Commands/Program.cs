using MARS.Commands.Services;
using MARS.Commands.Services.Adapters;
using MARS.Shared.Clients;
using MARS.Shared.Extensions;

namespace MARS.Commands;

public class Program
{
    private static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.AddMarsDefaults("MARS.Commands");

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
